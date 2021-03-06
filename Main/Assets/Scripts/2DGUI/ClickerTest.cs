using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// NOTE: Buttons might stop working after a while.
// BUG: Cannot move children while scaling.
// BUG: If you scale an object thats a child by the y (bigger) then hold transform on the child it'll zoom to you.

/// <summary>
/// ClickerTest: A class control what happens to the object with mouse-click actions. Attached to all spawned GameObjects.
/// </summary>
public class ClickerTest : MonoBehaviour {

    #region Variables
    // Shortcuts
    SelectorManagerScript SelectorManager;
    ButtonManagerScript ButtonManager;

    // Rotation
    private bool _markersSpawned = false;
    public bool IsLocked = false;
    public bool IsRotationLocked = false;
    GameObject arrowOne;
    GameObject arrowTwo;

    // Scaling
    public bool _Snapped = false;
    private float sizingFactor = 0.4f;
    private float closestFloat = 0;
    Vector3 startMouse;
    Vector3 initialScaling;
    Vector3 initialScalingPositions;
    public Axis GlobalAxis;
    public Axis LocalAxis;
    public enum Axis { x, y, z};

    // Moving and use for general mouse actions.
    Vector3 screenSpace;
    Vector3 mousePosition;
    Vector3 mouseInWorld;

    #endregion

    #region UnityMethods
    // Set color, initalize shortcuts
    private void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.gray);
        ButtonManager = GameObject.Find("ButtonManager").GetComponent<ButtonManagerScript>();
        SelectorManager = GameObject.Find("Plane").GetComponent<SelectorManagerScript>();
    }

    private void Update()
    {
        screenSpace = Camera.main.WorldToScreenPoint(transform.position);
        mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenSpace.z);
        mouseInWorld = Camera.main.ScreenToWorldPoint(mousePosition);

        // Switch to defualt color if not selected
        if (SelectorManager.selected != gameObject)
        {
            gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.gray);
        }

        // For rotation
        if (SelectorManager.selected == gameObject && ButtonManager.enabledButton == ButtonManagerScript.EnabledButton.RotateButton)
        {
            // Managing Markers.
            if (!_markersSpawned)
            {
                _markersSpawned = true;

                arrowOne = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                arrowOne.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
                arrowOne.transform.parent = gameObject.transform;
                Vector3 arrowOneTransform = new Vector3(0, 1f, 0);
                Quaternion arrowOneRotation = Quaternion.Euler(0, 0, 0);
                arrowOne.transform.localPosition = arrowOneTransform;
                arrowOne.transform.localRotation = arrowOneRotation;

                if (!IsRotationLocked && gameObject.GetComponent<ObjectJoint>() != null)
                {
                    arrowOne.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
                }

                if (!IsRotationLocked)
                {
                    arrowTwo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    arrowTwo.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
                    Quaternion arrowTwoRotation = Quaternion.Euler(0, 0, 90);
                    arrowTwo.transform.parent = gameObject.transform;
                    Vector3 arrowTwoTransform = new Vector3(1f, 0, 0);
                    arrowTwo.transform.localPosition = arrowTwoTransform;
                    arrowTwo.transform.localRotation = arrowTwoRotation;

                    Destroy(arrowTwo.GetComponent<Rigidbody>());
                    Destroy(arrowTwo.GetComponent<CapsuleCollider>());
                }
                Destroy(arrowOne.GetComponent<Rigidbody>());
                Destroy(arrowOne.GetComponent<CapsuleCollider>());
            }

            // Actual rotation managed.
            if (Input.GetMouseButton(0))
            {
                float rotSpeed = 5;
                float rotX = Input.GetAxis("Mouse X") * rotSpeed * Mathf.Deg2Rad;
                float rotY = Input.GetAxis("Mouse Y") * rotSpeed * Mathf.Deg2Rad;

                // How rotation should be if it's a joint
                if (gameObject.GetComponent<ObjectJoint>() != null)
                {
                    transform.RotateAround(Vector3.up, -rotX);
                    if (!IsRotationLocked)
                    {
                        transform.RotateAround(Vector3.right, rotY);
                    }
                }

                // How rotation should be if it's a link
                else
                {
                    if (!IsLocked)
                    {
                        transform.RotateAround(Vector3.up, -rotX);
                        transform.RotateAround(Vector3.right, rotY);
                    }
                    else
                    {
                        GameObject parent = gameObject.GetComponent<RobotLink>().ParentJoint;
                        parent.transform.RotateAround(Vector3.up, -rotX);
                        if (!parent.GetComponent<ClickerTest>().IsRotationLocked)
                        {
                            parent.transform.RotateAround(Vector3.right, rotY);
                        }
                    }
                }

            }
        }

        else
        {
            Destroy(arrowOne);
            Destroy(arrowTwo);
            _markersSpawned = false;
        }
    }

    // Selecting the object
    private void OnMouseDown()
    {
        SelectorManager.selected = gameObject;

        if (ButtonManager.enabledButton == ButtonManagerScript.EnabledButton.ScaleButton && SelectorManager.selected == gameObject)
        {
            startMouse = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z); // The position of the mouse when first clicked
            initialScaling = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z); // Scaling before changes
            initialScalingPositions = new Vector3(transform.position.x - transform.localScale.x / 2.0f, transform.position.y - transform.localScale.y / 2.0f, transform.position.z - transform.localScale.z / 2.0f); // Use for moving
        }
    }

    //Moving objects.
    private void OnMouseDrag()
    {
        if (ButtonManager.enabledButton == ButtonManagerScript.EnabledButton.TransformButton)
        {
            if (!IsLocked && !IsRotationLocked)
            {
                MoveObject(gameObject);
            }
            else
            {
                Debug.Log("hello");

                GameObject root = GetRootJoint(gameObject);

                screenSpace = Camera.main.WorldToScreenPoint(root.transform.position); // We need to get the root WorldToScreenPoint because we're moving the root.
                mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenSpace.z);
                mouseInWorld = Camera.main.ScreenToWorldPoint(mousePosition);

                MoveObject(root);
            }
        }

        if (ButtonManager.enabledButton == ButtonManagerScript.EnabledButton.ScaleButton && SelectorManager.selected == gameObject)
        {
            if (Input.mousePosition.x - startMouse.x != 0 || Input.mousePosition.y - startMouse.y != 0 || Input.mousePosition.z - startMouse.z != 0)
            {
                if (!_Snapped) // To make sure we scale only one side at a time
                {
                    SetLocalAxis();
                    SetGlobalAxis();
                    float mouseAmount = GetMouseAmount();
                    ScaleObject(mouseAmount);
                    _Snapped = true;
                }
                else
                {
                    float mouseAmount = GetMouseAmount();
                    float scaled = ScaleObject(mouseAmount);
                    ScaleMoveObject(scaled);
                }
            }
        }
    }

    private void OnMouseUp()
    {
        _Snapped = false;
        closestFloat = 0;
    }

    #endregion

    // ---------------------------------------------- Start of other methods ---------------------------------------------- //

    #region OtherMethods
    #region Scaling

    // This method gets the starting position/sizes then applys other methods to scale the gameObject.
    // Bug: Still scaling upon transform change.
    private void ChangeScale()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startMouse = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z); // The position of the mouse when first clicked
            initialScaling = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z); // Scaling before changes
            initialScalingPositions = new Vector3(transform.position.x - transform.localScale.x / 2.0f, transform.position.y - transform.localScale.y / 2.0f, transform.position.z - transform.localScale.z / 2.0f); // Use for moving
        }

        if (Input.GetMouseButton(0))
        {
            if (Input.mousePosition.x - startMouse.x != 0 || Input.mousePosition.y - startMouse.y != 0 || Input.mousePosition.z - startMouse.z != 0)
            {
                if (!_Snapped) // To make sure we scale only one side at a time
                {
                    SetLocalAxis();
                    SetGlobalAxis();
                    float mouseAmount = GetMouseAmount();
                    ScaleObject(mouseAmount);
                    _Snapped = true;
                }
                else
                {
                    float mouseAmount = GetMouseAmount();
                    float scaled = ScaleObject(mouseAmount);
                    ScaleMoveObject(scaled);
                }
            }
        }

        // Unsnap
        if (Input.GetMouseButtonUp(0))
        {
            _Snapped = false;
            closestFloat = 0;
        }
    }

    // Sets the local Axis of the object on click.
    private void SetLocalAxis()
    {
        Vector3 InverseVector = transform.InverseTransformPoint(mouseInWorld); // Get the position of the mouse relative to the object (shortcut I guess)

        if (InverseVector.x != 0 && System.Math.Abs(InverseVector.x) > closestFloat)
        {
            closestFloat = System.Math.Abs(InverseVector.x);
            LocalAxis = Axis.x;
        }

        if (InverseVector.y != 0 && System.Math.Abs(InverseVector.y) > closestFloat)
        {
            closestFloat = System.Math.Abs(InverseVector.y);
            LocalAxis = Axis.y;
        }

        if (InverseVector.z != 0 && System.Math.Abs(InverseVector.z) > closestFloat)
        {
            closestFloat = System.Math.Abs(InverseVector.z);
            LocalAxis = Axis.z;
        }
    }

    // Sets the global axis of the object on click.
    private void SetGlobalAxis()
    {
        float LargestDifference = 0;

        if (System.Math.Abs(mouseInWorld.x - transform.position.x) > LargestDifference)
        {
            LargestDifference = System.Math.Abs(mouseInWorld.x - transform.position.x);
            GlobalAxis = Axis.x;
        }
        if (System.Math.Abs(mouseInWorld.y - transform.position.y) > LargestDifference)
        {
            LargestDifference = System.Math.Abs(mouseInWorld.y - transform.position.y);
            GlobalAxis = Axis.y;
        }
        if (System.Math.Abs(mouseInWorld.z - transform.position.z) > LargestDifference)
        {
            LargestDifference = System.Math.Abs(mouseInWorld.z - transform.position.z);
            GlobalAxis = Axis.z;
        }
    }

    // Get the scale amount
    private float GetMouseAmount()
    {
        float returnValue = 0;

        switch (GlobalAxis)
        {
            case Axis.x:
                returnValue = mouseInWorld.x - transform.position.x;
                break;
            case Axis.y:
                returnValue = mouseInWorld.y - transform.position.y;
                break;
            case Axis.z:
                returnValue = mouseInWorld.z - transform.position.z;
                break;
        }

        return returnValue;
    }

    // Does actual scaling here
    private float ScaleObject(float scaleAmount)
    {
        // To be edited
        Vector3 EditScale = transform.localScale;
        float returnScale = 0;

        // Might want to make it abs scale (not negative scale)
        switch (LocalAxis)
        {
            case Axis.x:
                // Tbh idk how this stuff works...
                returnScale = initialScaling.x + (scaleAmount) * sizingFactor;
                EditScale.x = returnScale;
                break;
            case Axis.y:
                returnScale = initialScaling.y + (scaleAmount) * sizingFactor;
                EditScale.y = returnScale;
                break;
            case Axis.z:
                returnScale = initialScaling.z + (scaleAmount) * sizingFactor;
                EditScale.z = returnScale;
                break;
        }

        oppositeScaleChildren(EditScale, gameObject);

        return returnScale;
    }

    private void ScaleMoveObject(float scaleAmount)
    {
        Vector3 EditPosition = transform.position;

        switch (GlobalAxis)
        {
            case Axis.x:
                EditPosition.x = initialScalingPositions.x + transform.localScale.x / 2.0f;
                break;
            case Axis.y:
                EditPosition.y = initialScalingPositions.y + transform.localScale.y / 2.0f;
                break;
            case Axis.z:
                EditPosition.z = initialScalingPositions.z + transform.localScale.z / 2.0f;
                break;
        }

        transform.position = EditPosition;
    }

    // Scales the children in the opposite way such that they retain their "scale."
    // Intorduces "space" atm but doesnt seem like a scaling issue... It might scale the space around the objects just a little bit?
    //private void oppositeScaleChildren(Vector3 newScale)
    //{
    //    // Get all of the children then set their scale to the opposite reciprocal
    //    Transform[] childrenTransforms = GetComponentsInChildren<Transform>();
    //    foreach (Transform childTransform in childrenTransforms)
    //    {
    //        if (childTransform != transform)
    //        {
    //            Transform parent = childTransform.parent;
    //            childTransform.parent = null;
    //            Vector3 scaleTmp = childTransform.localScale;
    //            scaleTmp.x = scaleTmp.x / newScale.x;
    //            scaleTmp.y = scaleTmp.y / newScale. y;
    //            scaleTmp.z = scaleTmp.z / newScale.z;
    //            childTransform.parent = parent;
    //            childTransform.localScale = scaleTmp;
    //        }
    //    }

    //    // Set gameObject scale
    //    transform.localScale = newScale;
    //}


    private void oppositeScaleChildren(Vector3 newScale, GameObject thisObject)
    {
        Transform originalParent = transform.parent;

        foreach (Transform childTransform in thisObject.transform)
        {
            Transform parent = thisObject.transform;
            childTransform.parent = null;
            Vector3 scaleTmp = childTransform.localScale;
            scaleTmp.x = scaleTmp.x / newScale.x;
            scaleTmp.y = scaleTmp.y / newScale.y;
            scaleTmp.z = scaleTmp.z / newScale.z;
            childTransform.parent = parent;
            childTransform.localScale = scaleTmp;
            foreach (Transform child in childTransform)
            {
                // Problem: It keeps scaling the child for some reason?
                // And now this code stuff doesnt work huh
                //But why doesn't it work for the thing?
                //   Doesn't work if the parent is scaled for some reason... The grandchild goes a bit crazy on me (larger) for whatever reason
                //   So can we unparent from the parent then parent again?
                //   Now it gets smaller???
                oppositeScaleChildren(scaleTmp, childTransform.gameObject);
            }
        }

        transform.localScale = newScale;
    }

    //private void oppositeScaleChildren(Vector3 newScale, GameObject thisObject)
    //{
    //    Dictionary<Transform, Transform> parents = new Dictionary<Transform, Transform>();
    //    Transform[] childrenTransforms = GetComponentsInChildren<Transform>();
    //    int counter = 0;
    //    foreach (Transform childTransform in childrenTransforms)
    //    {
    //        parents.Add(childTransform, childTransform.parent);
    //        childTransform.parent = null;
    //    }

    //    transform.localScale = newScale;

    //    foreach(KeyValuePair<Transform, Transform> pairs in parents)
    //    {
    //        pairs.Key.parent = pairs.Value;
    //    }
    //}
    #endregion

    // Method for moving the object
    private void MoveObject(GameObject movingObject)
    {
        movingObject.transform.position = mouseInWorld;
    }

    // Method for getting the rootJoint recursively (Used for moving locked object)
    private static GameObject GetRootJoint(GameObject thisObject)
    {

        // If it's a joint
        if (thisObject.GetComponent<ObjectJoint>() != null)
        {
            if (thisObject.GetComponent<ObjectJoint>().ParentJoint != null)
            {
                return GetRootJoint(thisObject.GetComponent<ObjectJoint>().ParentJoint);
            }
            else
            {
                return thisObject;
            }
        }

        // If it's a link
        else if (thisObject.GetComponent<RobotLink>() != null)
        {
            if (thisObject.GetComponent<RobotLink>().ParentJoint != null)
            {
                return GetRootJoint(thisObject.GetComponent<RobotLink>().ParentJoint);
            }
            else
            {
                return thisObject;
            }
        }

        else
        {
            return null;
        }
    }

    #endregion
}

// Note: Need root joint if we want to test transform.