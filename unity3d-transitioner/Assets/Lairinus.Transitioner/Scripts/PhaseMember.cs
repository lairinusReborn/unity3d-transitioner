﻿using Lairinus.Transitions.Internal;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Lairinus.Transitions
{
    public enum MemberType
    {
        Property,
        Field
    }

    [System.Serializable]
    public class PhaseMember
    {
        public PhaseMember(MemberType memberType, Component _parentComponent, string _memberName, Type type)
        {
            _sf_memberType = memberType;
            _sf_parentComponent = _parentComponent;
            _sf_memberName = _memberName;
            if (Utility.GetInstance().typesDictionary.ContainsKey(type))
                _sf_serializedPropertyType = Utility.GetInstance().typesDictionary[type];
        }

        [SerializeField] private MemberType _sf_memberType = MemberType.Field;
        [SerializeField] private Utility.AvailableMemberTypes _sf_serializedPropertyType = new Utility.AvailableMemberTypes();
        [SerializeField] private Component _sf_parentComponent = null;
        [SerializeField] private string _sf_memberName = "";
        [SerializeField] private string _sf_memberValueString = "";
        [SerializeField] private bool _sf_isDisabled = false;
        [SerializeField] private bool _sf_canBeLerped = false;
        [SerializeField] private bool _sf_useSeparateAnimationCurve = false;
        [SerializeField] private AnimationCurve _sf_animationCurve = new AnimationCurve();
        public bool canBeLerped { get { return _sf_canBeLerped; } }
        public bool useSeparateAnimationCurve { get { return _sf_useSeparateAnimationCurve; } }
        protected string memberValueString { get { return _sf_memberValueString; } }
        public Component parentComponent { get { return _sf_parentComponent; } }
        public string memberName { get { return _sf_memberName; } }
        public PropertyInfo propertyInfo { get; private set; }
        public AnimationCurve separateAnimationCurve { get { return _sf_animationCurve; } }
        public FieldInfo fieldInfo { get; private set; }
        public Utility.AvailableMemberTypes serializedMemberType { get { return _sf_serializedPropertyType; } }
        public bool isDisabled { get { return _sf_isDisabled; } }
        private object _startValue = null;
        private object _resetValue = null;
        private object _finalValue = null;
        private bool _isInitialized = false;

        public void SetValue(object value)
        {
            try
            {
                if (!_isInitialized)
                    Initialize();
                if (fieldInfo == null && propertyInfo == null)
                    TryCacheReflectedMemberFields();

                if (_sf_memberType == MemberType.Field && fieldInfo != null)
                    fieldInfo.SetValue(_sf_parentComponent, value);
                else if (_sf_memberType == MemberType.Property && propertyInfo != null)
                    propertyInfo.SetValue(_sf_parentComponent, value, null);
            }
            catch
            {
            }
        }

        public object GetValue()
        {
            if (!_isInitialized)
                Initialize();
            if (_sf_memberType == MemberType.Field && fieldInfo != null)
                return fieldInfo.GetValue(_sf_parentComponent);
            else if (_sf_memberType == MemberType.Property && propertyInfo != null)
                return propertyInfo.GetValue(_sf_parentComponent, null);
            else return new object();
        }

        private void Initialize()
        {
            _isInitialized = true;
            TryCacheReflectedMemberFields();
            if (_startValue == null)
                _startValue = GetValue();

            if (_resetValue == null)
                _resetValue = _startValue;

            _finalValue = Utility.GetObject_Runtime(_sf_serializedPropertyType, _sf_memberValueString);
        }

        public void UpdatePhaseMember(float curveTime, float actualTime, bool isResetting)
        {
            if (_sf_isDisabled)
                return;

            if (!_isInitialized || isResetting)
            {
                _isInitialized = true;
                Initialize();
            }

            if (isResetting)
                _startValue = _resetValue;

            if (_startValue == null)
                _startValue = GetValue();

            float lerpValueFloat = curveTime;
            if (_sf_useSeparateAnimationCurve)
                lerpValueFloat = _sf_animationCurve.Evaluate(actualTime);

            if (_sf_canBeLerped)
            {
                // Get string currentValue from dictionary
                // Get finalValue on initialization
                object lerpedValue = Utility.GetLerpedValue_Runtime(_startValue, _finalValue, _sf_serializedPropertyType, lerpValueFloat);
                SetValue(lerpedValue);
                if (lerpValueFloat >= 1)
                    _startValue = null;
            }
            else if (lerpValueFloat >= 1)
            {
                object finalValue = Utility.GetObject(Utility.GetInstance().reverseTypeDictionary[_sf_serializedPropertyType], _sf_memberValueString);
                SetValue(finalValue);
                _startValue = null;
            }
        }

        private void TryCacheReflectedMemberFields()
        {
            // Reduces some of the burden of Reflection which is highly appreciated considering
            // that Reflection.Emit() is unusable for iOS because of AOT vs JIT limitations... :(
            if (_sf_parentComponent == null)
                return;

            if (_sf_memberType == MemberType.Property)
            {
                if (propertyInfo == null)
                    propertyInfo = _sf_parentComponent.GetType().GetProperty(_sf_memberName);
            }
            else
            {
                if (fieldInfo == null)
                    fieldInfo = _sf_parentComponent.GetType().GetField(_sf_memberName);
            }
        }

        public MemberType memberType { get { return _sf_memberType; } }

        public object GetMemberValue(SerializedPropertyType type)
        {
            return null;
        }
    }
}