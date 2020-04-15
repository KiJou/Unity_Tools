using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DynamicBoneManager : MonoBehaviour
{
    public enum WindDirection
    {
        Forward,
        BackWard,
        Right,
        Left,
        Up,
        Down,
    }

    static class WindData
    {
        static Vector3 ForwardWind  = Vector3.forward;
        static Vector3 BackwardWind = Vector3.back;
        static Vector3 RightWind = Vector3.right;
        static Vector3 LeftWind = Vector3.left;
        static Vector3 UpWind   = Vector3.up;
        static Vector3 DownWind = Vector3.down;

        public static Vector3 DirectionConvertVector(WindDirection direction)
        {
            switch (direction)
            {
                case WindDirection.Forward:
                    return ForwardWind;
                case WindDirection.BackWard:
                    return BackwardWind;
                case WindDirection.Right:
                    return RightWind;
                case WindDirection.Left:
                    return LeftWind;
                case WindDirection.Up:
                    return UpWind;
                case WindDirection.Down:
                    return DownWind;
            }
            return Vector3.zero;
        }
    }

    [Header("影響度")]
    [SerializeField, Range(0f, 1f)]
    private float dynamicBoneWeight = 1f;

    [Header("空気抵抗係数")]
    [SerializeField, Range(-50f, 50f)]
    private float coefficient = 3f;

    [Header("方向")]
    [SerializeField]
    private WindDirection direction = WindDirection.Forward;

    [Header("風機能on/off")]
    [SerializeField]
    private bool useWind = false;

    private List<DynamicBone> dynamicBoneArray = new List<DynamicBone>();
    private Vector3 directionVector;

    private void Awake()
    {
        this.dynamicBoneArray = FindObjectsOfType<DynamicBone>().ToList();
    }

    private void LateUpdate()
    {
        this.directionVector = WindData.DirectionConvertVector(this.direction);
        foreach (DynamicBone db in this.dynamicBoneArray)
        {
            if (!db || !db.enabled)
            {
                continue;
            }
            db.SetWeight(this.dynamicBoneWeight);
            db.m_UseWind = this.useWind;
            db.m_WindDirection = this.directionVector * this.coefficient;
        }
    }

}
