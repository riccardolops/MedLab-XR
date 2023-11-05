using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Examples.Demos;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityVolumeRendering;

[RequireComponent(typeof(SolverTrackedTargetType))]
[RequireComponent(typeof(SolverHandler))]
public class ControllerMenuHandler : MonoBehaviour
{
    [SerializeField]
    private GameObject MenuContent;
    [SerializeField]
    private GameObject ButtonClose;
    [SerializeField]
    private GameObject sphereFollow;
    private VolumeRenderedObject volObj;
    private SolverTrackedTargetType solverTrackedTargetType;
    private SolverHandler solverHandler;
    private HandConstraintPalmUp handConstraintPalmUp;
    private Follow follow;
    private Follow followController;
    private SolverHandler solverTool;
    private void Start()
    {
        solverTrackedTargetType = gameObject.GetComponent<SolverTrackedTargetType>();
        solverHandler = gameObject.GetComponent<SolverHandler>();
        handConstraintPalmUp = gameObject.GetComponent<HandConstraintPalmUp>();
        follow = gameObject.GetComponent<Follow>();
        sphereFollow = GameObject.Instantiate(Resources.Load<GameObject>("CutoutSphere"));
        sphereFollow.gameObject.GetComponent<CutoutSphere>().CutoutType = CutoutType.Exclusive;
        sphereFollow.transform.rotation = Quaternion.Euler(270.0f, 0.0f, 0.0f);
        sphereFollow.transform.localScale = sphereFollow.transform.localScale * 0.5f;
        solverTool = sphereFollow.AddComponent<SolverHandler>();
        solverTool.TrackedTargetType = TrackedObjectType.ControllerRay;
        followController = sphereFollow.AddComponent<Follow>();
        followController.MinDistance = 0.1f;
        followController.MaxDistance = 0.1f;
        followController.DefaultDistance = 0.1f;
        sphereFollow.SetActive(false);
    }
    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            if (OVRInput.GetActiveController() == OVRInput.Controller.LTouch || OVRInput.GetActiveController() == OVRInput.Controller.RTouch || OVRInput.GetActiveController() == OVRInput.Controller.Touch)
            {
                handConstraintPalmUp.enabled = false;
                follow.enabled = true;
                solverTrackedTargetType.ChangeTrackedTargetTypeControllerRay();
                solverHandler.AdditionalOffset = new Vector3(0.1f, 0.2f, 0.0f);
                solverHandler.UpdateSolvers = true;
                MenuContent.SetActive(!MenuContent.activeInHierarchy);
                ButtonClose.SetActive(!ButtonClose.activeInHierarchy);
            }
            else if (OVRInput.GetActiveController() == OVRInput.Controller.LHand || OVRInput.GetActiveController() == OVRInput.Controller.RHand || OVRInput.GetActiveController() == OVRInput.Controller.Hands)
            {
                follow.enabled = false;
                handConstraintPalmUp.enabled = true;
                solverTrackedTargetType.ChangeTrackedTargetTypeHandJoint();
                solverHandler.AdditionalOffset = new Vector3(0.0f, -0.1f, 0.05f);
                solverHandler.UpdateSolvers = true;
                MenuContent.SetActive(!MenuContent.activeInHierarchy);
                ButtonClose.SetActive(!ButtonClose.activeInHierarchy);
            }
        }
        else if (OVRInput.GetDown(OVRInput.Button.Three) || OVRInput.GetDown(OVRInput.Button.One))
        {
            if (OVRInput.GetActiveController() == OVRInput.Controller.LTouch || OVRInput.GetActiveController() == OVRInput.Controller.RTouch || OVRInput.GetActiveController() == OVRInput.Controller.Touch)
            {
                sphereFollow.SetActive(true);
            }
        }
        else if (OVRInput.GetUp(OVRInput.Button.Three) || OVRInput.GetUp(OVRInput.Button.One))
        {
            if (OVRInput.GetActiveController() == OVRInput.Controller.LTouch || OVRInput.GetActiveController() == OVRInput.Controller.RTouch || OVRInput.GetActiveController() == OVRInput.Controller.Touch)
            {
                sphereFollow.SetActive(false);
            }
        }
    }
    public void SetObj(VolumeRenderedObject obj)
    {
        volObj = obj;
        sphereFollow.gameObject.GetComponent<CutoutSphere>().SetTargetObject(volObj);
    }
}
