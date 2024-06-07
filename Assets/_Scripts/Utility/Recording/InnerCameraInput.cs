//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.5.1
//     from Assets/_Scripts/Utility/Recording/InnerCameraInput.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @InnerCameraInput: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @InnerCameraInput()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""InnerCameraInput"",
    ""maps"": [
        {
            ""name"": ""Recording"",
            ""id"": ""4fbeaeb5-c28c-4d5b-8431-67b7287121e6"",
            ""actions"": [
                {
                    ""name"": ""MoveCamera"",
                    ""type"": ""Value"",
                    ""id"": ""e2563e24-ba13-4d6c-b12d-20b819062518"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""Keyboard"",
                    ""id"": ""97590170-ccc7-46d3-bcd1-8c1a07101d34"",
                    ""path"": ""2DVector(mode=2)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Up"",
                    ""id"": ""03fc0c0c-45fa-4196-9d1d-1588bfae567c"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Down"",
                    ""id"": ""0d719d5a-b427-4728-8975-3e7e3aac34a1"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Left"",
                    ""id"": ""f37111fe-b546-45f7-92bd-dd00c95e098b"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Right"",
                    ""id"": ""f8a94c9f-50b2-4172-8048-886058d87668"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""f1879c2d-b5aa-43fc-8b84-f903613a8e4b"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Recording
        m_Recording = asset.FindActionMap("Recording", throwIfNotFound: true);
        m_Recording_MoveCamera = m_Recording.FindAction("MoveCamera", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Recording
    private readonly InputActionMap m_Recording;
    private List<IRecordingActions> m_RecordingActionsCallbackInterfaces = new List<IRecordingActions>();
    private readonly InputAction m_Recording_MoveCamera;
    public struct RecordingActions
    {
        private @InnerCameraInput m_Wrapper;
        public RecordingActions(@InnerCameraInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @MoveCamera => m_Wrapper.m_Recording_MoveCamera;
        public InputActionMap Get() { return m_Wrapper.m_Recording; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(RecordingActions set) { return set.Get(); }
        public void AddCallbacks(IRecordingActions instance)
        {
            if (instance == null || m_Wrapper.m_RecordingActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_RecordingActionsCallbackInterfaces.Add(instance);
            @MoveCamera.started += instance.OnMoveCamera;
            @MoveCamera.performed += instance.OnMoveCamera;
            @MoveCamera.canceled += instance.OnMoveCamera;
        }

        private void UnregisterCallbacks(IRecordingActions instance)
        {
            @MoveCamera.started -= instance.OnMoveCamera;
            @MoveCamera.performed -= instance.OnMoveCamera;
            @MoveCamera.canceled -= instance.OnMoveCamera;
        }

        public void RemoveCallbacks(IRecordingActions instance)
        {
            if (m_Wrapper.m_RecordingActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IRecordingActions instance)
        {
            foreach (var item in m_Wrapper.m_RecordingActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_RecordingActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public RecordingActions @Recording => new RecordingActions(this);
    public interface IRecordingActions
    {
        void OnMoveCamera(InputAction.CallbackContext context);
    }
}
