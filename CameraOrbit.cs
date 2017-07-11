using UnityEngine;
using System.Collections;

public delegate void CameraCallbackType(RaycastHit hit);
public delegate void CameraCallbackPostRender(int layer);

public class CameraOrbit : MonoBehaviour
{
    #region types

	[System.Serializable]
    public class XAxis
    {
        public  float   Value;
        public  float   Goal;
		public	float	DefaultValue;
        //public  bool    useLimits = false;
        public  float   Max = 0.0f;
        public  float   Min = 6.28f;
		public	float	SmoothSpeed = 10.0f;
        public float    InputSpeed = 0.01f;
    };

    [System.Serializable]
    public class YAxis
    {
        public float Value;
        public float Goal;
        public float DefaultValue = 0.3f;
        //public bool useLimits = true;
        public float Max = 1.7f;
        public float Min = 0.1f;
        public float SmoothSpeed = 10.0f;
        public float InputSpeed = 0.01f;
    };

    [System.Serializable]
    public class ZAxis
    {
        public float Value;
        public float Goal;
        public float DefaultValue = 50.0f;
        //public bool useLimits = false;
        public float Max = 100.0f;
        public float Min = 0f;
        public float SmoothSpeed = 10.0f;
        public float InputSpeed = 10.0f;
    };
    #endregion

    #region publics

    public  Transform   _Target;
	public	KeyCode		_ActionRotate = KeyCode.Mouse0;
	public	KeyCode		_ActionSelect = KeyCode.Mouse1;
	public	KeyCode		_ActionCTRL = KeyCode.LeftControl;

	public	XAxis		_Horizontal;
	public	YAxis		_Vertical;
	public	ZAxis		_Zoom;

	public	bool		_AutoRegisterForGL;

    #endregion

    #region privates

    public static CameraOrbit _instance = null;

    // Camera
    private Vector3     _vCenterTarget;
    private Vector3     _vCenterUsed;
    private Vector3     _vCenterVelocity;
    private Vector3     _vPos;

    private bool        bFree;
    private bool        bDrag;

    private Vector3     DragOffset;
    private Vector3     DragPrevious;

	private	Vector3		ScreenCenter;

    // Pick
	private	CameraCallbackType _Picked_CallBack;
	private bool _IsLock = false;

	//Render
	private CameraCallbackPostRender _PostRender_CallBack;

    #endregion

    #region behaviour

    void Awake()
    {
		_instance = this;

        _Horizontal.Goal = _Horizontal.DefaultValue;
        _Vertical.Goal = _Vertical.DefaultValue;
        _Zoom.Goal = _Zoom.DefaultValue;

		if (_AutoRegisterForGL)
		{
			RegisterPostRenderCallback(GL3D.Instance.DrawNow);
		}
    }

    public static CameraOrbit Instance { get { return _instance; } }

    //****************************************************************************
    //
    void Start()
    {
		ScreenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0);
		_vCenterUsed = _vCenterTarget = _Target.position;
    }

	//****************************************************************************
	//
	void OnDrawGizmosSelected()
	{
		if (_Target != null)
		{
			Gizmos.DrawWireSphere(_Target.position, _Zoom.Max);
			Gizmos.DrawWireSphere(_Target.position, _Zoom.Min);
		}
	}

    //****************************************************************************
    //
    void Update()
    {
		if (!_IsLock)
		{
			DoDrag();
			DoCamera();
		}
    }

    void LateUpdate()
    {
		FindObject();
    }
    #endregion

    #region code

    //****************************************************************************************
    //
    //****************************************************************************************

    private void DoDrag()
    {
		if (Input.GetKeyDown(_ActionRotate) && Input.mousePosition.y < Screen.height - Screen.height / 10.0f)
        {
            bDrag = true;
            DragPrevious = Input.mousePosition;
        }

        if (bDrag)
        {
			if (Input.GetKeyUp(_ActionRotate))
            {
                DragOffset = Vector3.zero;
                bDrag = false;
            }
            else
            {
                DragOffset = DragPrevious - Input.mousePosition;
                DragPrevious = Input.mousePosition;
            }
        }
    }

    //****************************************************************************
    //
	
    public void DoCamera()
    {
        _vCenterUsed = _Target.position;// Vector3.SmoothDamp(_vCenterUsed, _vCenterTarget, ref _vCenterVelocity, 0.5f);

		_vPos.x = Mathf.Sin(_Horizontal.Value) * Mathf.Cos(_Vertical.Value) * _Zoom.Value;
		_vPos.y = Mathf.Sin(_Vertical.Value) * _Zoom.Value;
		_vPos.z = Mathf.Cos(_Horizontal.Value) * Mathf.Cos(_Vertical.Value) * _Zoom.Value;

        transform.position = _Target.TransformPoint(_vPos);
		transform.LookAt(_vCenterUsed, _Target.up);

        ComputeCamera();
    }
    //****************************************************************************
    //

    public void ComputeCamera()
    {
        // Rotation

        _Horizontal.DefaultValue -= DragOffset.x * _Horizontal.InputSpeed;														                        // Set real
        _Horizontal.Value = Mathf.Lerp(_Horizontal.Value, _Horizontal.DefaultValue, Time.deltaTime * _Horizontal.SmoothSpeed);	// Lerp final from real

        // Elevation

        _Vertical.Goal += DragOffset.y * _Vertical.InputSpeed;														                            // Set real
        _Vertical.Goal = Mathf.Min(Mathf.Max(_Vertical.Goal, _Vertical.Min), _Vertical.Max);			        	            // Limit real
        _Vertical.Value = Mathf.Lerp(_Vertical.Value, _Vertical.Goal, Time.deltaTime * _Vertical.SmoothSpeed);	            // Lerp final from real

        // Zoom

        _Zoom.Goal -= Input.GetAxis("Mouse ScrollWheel") * _Zoom.InputSpeed;								                            // Set real
        _Zoom.Goal = Mathf.Min(Mathf.Max(_Zoom.Goal, _Zoom.Min), _Zoom.Max);							    		            // Limit real
        _Zoom.Value = Mathf.Lerp(_Zoom.Value, _Zoom.Goal, Time.deltaTime * _Zoom.SmoothSpeed);					        	// Lerp final from real

    }

    //****************************************************************************
    //
    private void FindObject()
    {
        RaycastHit hit;
		Ray ray = this.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

        //Debug.DrawRay(ray.origin, ray.direction * 10000.0f, Color.yellow);

        if (Physics.Raycast(ray, out hit, 10000.0f))
        {
			if (Input.GetKeyDown(_ActionSelect))
				_vCenterTarget = hit.point;

			if (_Picked_CallBack != null)
				_Picked_CallBack(hit);
        }
    }

	public void RegisterPickCallback(CameraCallbackType callback)
	{
		Debug.Log("RegisterPickCallback : " + transform.name);
		_Picked_CallBack = callback;
	}

	public void Lock(bool islock)
	{
		_IsLock = islock;
	}

	public void RegisterPostRenderCallback(CameraCallbackPostRender callback)
	{
		Debug.Log("RegisterPostRenderCallback : " + transform.name);
		_PostRender_CallBack = callback;
	}

	void OnPostRender()
	{
		if (_PostRender_CallBack != null)
		{
			if (_AutoRegisterForGL)
				_PostRender_CallBack(1);
			else
				_PostRender_CallBack(2);
		}
	}

    #endregion
}
