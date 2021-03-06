﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ButtonManagerScript: A class to keep track of and manage button states.
///
/// Attributes:
///     JointButton: A reference to the Button that spawns a joint.
///		LinkButton: A reference to the Button that spawns a link.
///		MoveButton: A reference to the Button that allows the user to move camera.
///		TransformButton: A reference to the Button that allows the user to move the selected object.
///		ScaleButton: A reference to the Button that allows the user to scale the selected object.
///		RotateButton: A reference to the Button that allows the user to rotate the selected object.
///		AttachButton: A reference to the Button that allows the user to attach the selected objet to another object.
///		enabledButton: An enum indicating which Button was last pressed (and is thus enabled).
/// </summary>
public class ButtonManagerScript : MonoBehaviour {

    // Linking all of the buttons here
    public Button JointButton;
    public Button LinkButton;
    public Button MoveButton;
    public Button TransformButton;
    public Button ScaleButton;
    public Button RotateButton;
    public Button AttachButton;

    // Used to use specific modes of manipulation.
    public EnabledButton enabledButton;

    // Button states
    public enum EnabledButton
    {
        MoveButton,
        TransformButton,
        ScaleButton,
        RotateButton
    };

    // Connect buttons to the methods
    private void Start()
    {
        JointButton.onClick.AddListener(SpawnJoint);
        LinkButton.onClick.AddListener(SpawnLink);
        MoveButton.onClick.AddListener(EnableCameraMovement);
        TransformButton.onClick.AddListener(EnableTransform);
        ScaleButton.onClick.AddListener(EnableScaling);
        RotateButton.onClick.AddListener(EnableRotation);
        AttachButton.onClick.AddListener(Attach);
    }

    #region Button Methods
    private void EnableTransform()
    {
        enabledButton = EnabledButton.TransformButton;
    }

    private void EnableCameraMovement()
    {
        enabledButton = EnabledButton.MoveButton;
    }

    private void EnableScaling()
    {
        enabledButton = EnabledButton.ScaleButton;
    }

    private void EnableRotation()
    {
        enabledButton = EnabledButton.RotateButton;
    }

    private void SpawnJoint()
    {
        GameObject newJoint = ObjectJoint.SpawnJoint();
        newJoint.AddComponent<ClickerTest>();
    }

    private void SpawnLink()
    {
        GameObject newLink = RobotLink.SpawnLink();
        newLink.AddComponent<ClickerTest>();
    }

	/// <summary>
	/// Attach: Attaches the currently selected objet to the nearest available link/joint depending on what is selected.
	/// </summary>
    private void Attach()
    {
        GameObject selected = GameObject.Find("Plane").GetComponent<SelectorManagerScript>().selected;

        // Checking if object is selected
        if (selected != null)
        {
            // If selected object is a link
            if (selected.GetComponent<RobotLink>() != null && !selected.GetComponent<ClickerTest>().IsLocked)
            {
                GameObject closestJoint = GetClosestJoint(selected);

                // If there a joint exists, link it to the closest one.
                if (closestJoint != null)
                {
                    // ObjectJoint/RobotLink procedures
                    closestJoint.GetComponent<ObjectJoint>().ChildLink = selected;
                    selected.GetComponent<RobotLink>().ParentJoint = closestJoint;

                    selected.transform.parent = closestJoint.transform;

                    // "Locking" the object
                    selected.GetComponent<ClickerTest>().IsLocked = true;
                    closestJoint.GetComponent<ClickerTest>().IsLocked = true;
                }
                else
                {
                    Debug.Log("You need a Joint to attach the Link to!");
                }
            }

            // If selected object is a joint
            else if (selected.GetComponent<ObjectJoint>() != null)
            {
                GameObject closestLink = GetClosestLink(selected);

                // If there is a link, attach it to the closest one.
                if (closestLink != null)
                {
                    ObjectJoint thisJoint = selected.GetComponent<ObjectJoint>();

                    // Doing ObjectJoint/RobotLink stuff
                    thisJoint.ParentJoint = closestLink.GetComponent<RobotLink>().ParentJoint;
                    thisJoint.ParentJoint.GetComponent<ObjectJoint>().ChildJoints.Add(selected);
                    thisJoint.ParentLink = closestLink;

                    selected.transform.parent = closestLink.transform;

                    // "Locking" the object
                    selected.GetComponent<ClickerTest>().IsRotationLocked = true;
                }
                else
                {
                    Debug.Log("You need a Link to attach the Joint to!");
                }
            }
        }
    }
    #endregion

    #region Other Methods
    // Getting the closest joint (returns null if no ObjectJoints)
    private GameObject GetClosestJoint(GameObject link)
    {
        GameObject closest = null;
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject GO in allObjects)
        {
            if (GO.GetComponent<ObjectJoint>() != null && GO.activeInHierarchy && !GO.GetComponent<ClickerTest>().IsLocked)
            {
                if (closest == null)
                {
                    closest = GO;
                }
                else
                {
                    if (Vector3.Distance(link.transform.position, GO.transform.position) < Vector3.Distance(link.transform.position, closest.transform.position))
                    {
                        closest = GO;
                    }
                }
            }
        }
        return closest;
    }

    // Duplicate of Joint version
    private GameObject GetClosestLink(GameObject joint)
    {
        GameObject closest = null;
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject GO in allObjects)
        {
            if (GO.GetComponent<RobotLink>() != null && GO.activeInHierarchy)
            {
                if (closest == null)
                {
                    closest = GO;
                }
                else
                {
                    if (Vector3.Distance(joint.transform.position, GO.transform.position) < Vector3.Distance(joint.transform.position, closest.transform.position))
                    {
                        closest = GO;
                    }
                }
            }
        }
        return closest;
    }
    #endregion

}
