using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Features.Revised_Controller
{
    public class TestController : MonoBehaviour
    {
        private void Start()
        {
            ControlsManager.Instance.Subscribe(this, "Space", ControlsManager.ControlEvent.OnPressed, Space);
        }

        private void Space()
        {
            Debug.Log("Space pressed");
        }

        [Button]
        private void Clear()
        {
            ControlsManager.Instance.UnsubscribeAll(this);
        }
    }
}