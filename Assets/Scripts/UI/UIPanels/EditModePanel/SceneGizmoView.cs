using System;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class SceneGizmoView : MonoBehaviour
{
    public enum EAxisSign
    {
        Positive = 0,
        Negative
    }
    public enum EAxisName
    {
        X,
        Y,
        Z,
    }
    public enum EAxisType
    {
        None,
        PositiveXAxis,
        NegativeXAxis,
        PositiveYAxis,
        NegativeYAxis,
        PositiveZAxis,
        NegativeZAxis,
    }
    public class AxisDes
    {
        private EAxisSign mSign;
        private EAxisName mIndex;

        public EAxisSign Sign { get { return mSign; } }
        public EAxisName Index { get { return mIndex; } }
        public bool IsPositive { get { return mSign == EAxisSign.Positive; } }
        public bool IsNegative { get { return mSign == EAxisSign.Negative; } }

        public AxisDes(EAxisName axisIndex, EAxisSign axisSign)
        {
            mSign = axisSign;
            mIndex = axisIndex;
        }
    }
    public class SceneGizmoAxis
    {
        AxisDes mAxisDesc;
        public Action<EAxisType, AxisDes> mOnClickCallBack;
        public GameObject mGameObject;
        public Button mBtn;
        public EAxisType mAxisType;

        public SceneGizmoAxis(GameObject obj, EAxisType axisType, AxisDes gizmoAxisDesc, Action<EAxisType, AxisDes> OnClickCallBack)
        {
            mAxisDesc = gizmoAxisDesc;
            mOnClickCallBack = OnClickCallBack;
            mGameObject = obj;
            mAxisType = axisType;
            Find();
        }

        public void Find()
        {
            mBtn = mGameObject.transform.Find("Button").GetComponent<Button>();
            mBtn.onClick.AddListener(OnClick);
        }
        public void OnClick()
        {
            mOnClickCallBack?.Invoke(mAxisType, mAxisDesc);
        }
    }

    private Dictionary<EAxisType, SceneGizmoAxis> mSceneGizmoAxis = new Dictionary<EAxisType, SceneGizmoAxis>();
    public Vector3[] mAxes3D = new Vector3[3];
    public EAxisType mCurrentAxisType = EAxisType.None;
    private GameObject mGizmoRoot;
    private Button mSwitchBtn;
    private Button mBgBtn;
    private bool mGizmoIsVisible = false;
    public bool GizmoIsVisible
    {
        get
        { return mGizmoIsVisible; }
        set
        {
            mGizmoRoot.SetActive(value);
            mBgBtn.gameObject.SetActive(value);
            DeselectedCurAxis();
            mGizmoIsVisible = value;
        }
    }
    public void Start()
    {
        mSwitchBtn = transform.Find("Panel/BtnGroup/Button").GetComponent<Button>();
        mSwitchBtn.onClick.AddListener(OnClickSwitchBtn);
        mGizmoRoot = transform.Find("Panel/GizmoGroup").gameObject;
        mBgBtn = transform.Find("Panel/BgBtn").GetComponent<Button>();
        mBgBtn.onClick.AddListener(OnClickBgBtn);

        GameObject xAxis = transform.Find("Panel/GizmoGroup/X").gameObject;
        GameObject yAxis = transform.Find("Panel/GizmoGroup/Y").gameObject;
        GameObject zAxis = transform.Find("Panel/GizmoGroup/Z").gameObject;

        mSceneGizmoAxis.Add(EAxisType.PositiveXAxis, new SceneGizmoAxis(xAxis, EAxisType.PositiveXAxis, new AxisDes(EAxisName.X, EAxisSign.Positive), OnGizmoHandlePicked));
        mSceneGizmoAxis.Add(EAxisType.PositiveYAxis, new SceneGizmoAxis(yAxis, EAxisType.PositiveYAxis, new AxisDes(EAxisName.Y, EAxisSign.Positive), OnGizmoHandlePicked));
        mSceneGizmoAxis.Add(EAxisType.PositiveZAxis, new SceneGizmoAxis(zAxis, EAxisType.PositiveZAxis, new AxisDes(EAxisName.Z, EAxisSign.Positive), OnGizmoHandlePicked));

        UpdateAxis();
        GizmoIsVisible = false;
    }
    private void DeselectedCurAxis()
    {
        SceneGizmoAxis axis = null;
        if (mSceneGizmoAxis.TryGetValue(mCurrentAxisType, out axis))
        {
            mCurrentAxisType= EAxisType.None;
        }
    }
    private void OnGizmoHandlePicked(EAxisType axisType, AxisDes _axisDesc)
    {
        Quaternion targetRotation = Quaternion.LookRotation(-GetAxis3D(_axisDesc), Vector3.up);
        InputHandlerManager.Inst.SetSceneGizmoRotation(targetRotation);
        if (mCurrentAxisType != axisType)
        {
            mCurrentAxisType = axisType;
        }
        OnClickBgBtn();
    }
    public void UpdateAxis()
    {
        Matrix4x4 rotationMtx = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        mAxes3D[0] = GetNormalizedAxis(rotationMtx, 0);
        mAxes3D[1] = GetNormalizedAxis(rotationMtx, 1);
        mAxes3D[2] = GetNormalizedAxis(rotationMtx, 2);
    }
    public static Vector3 GetNormalizedAxis(Matrix4x4 matrix, int axisIndex)
    {
        Vector3 axis = matrix.GetColumn(axisIndex);
        return Vector3.Normalize(axis);
    }
    public Vector3 GetAxis3D(AxisDes axisDesc)
    {
        Vector3 axis = mAxes3D[(int)axisDesc.Index];
        if (axisDesc.IsNegative) axis = -axis;
        return axis;
    }

    public void OnClickSwitchBtn()
    {
        GizmoIsVisible = !GizmoIsVisible;
    }
    public void OnClickBgBtn()
    {
        if (GizmoIsVisible) GizmoIsVisible = false;
    }
    public void HandleMouseAndKeyboardInput()
    {
        DeselectedCurAxis();
    }
}