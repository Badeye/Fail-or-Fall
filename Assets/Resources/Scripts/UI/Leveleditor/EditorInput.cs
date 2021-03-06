﻿using FlipFall.Audio;
using FlipFall.LevelObjects;
using FlipFall.Levels;
using FlipFall.Progress;
using FlipFall.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles zooming, moving and vertex adding input inside the editor
/// zoom by using the numpad + and - keys in Unity or pinch the screen on mobile.
/// </summary>

namespace FlipFall.Editor
{
    public class EditorInput : MonoBehaviour
    {
        public float zoomSpeed = 20f;        // The rate of change of the orthographic size in orthographic mode.
        public float panThreshold = 0.75F;

        public float minSize = 500F;
        public float maxSize = 3000F;

        // ignores any input from the bottom of the screen up in percent, important for vertex adding and object placement
        public float ignoreBottomScreenPercent = 0.2F;

        // only relevant on the PC, zoom factor for each keypress
        public float keyboardZoomStep = 250F;

        private Camera cam;

        // vertex getting dragged
        public static bool vertexDragged = false;

        private Vector2 currentPosition;
        private Vector2 deltaPositon;
        private Vector2 lastPositon;

        // the time needed between to clicks to account for a double click/tap
        public float doubleClickDelay = 0.3F;

        // the time of the last registered click
        private float doubleClickTime;

        // levelobject getting dragged
        private bool objectDragged = true;

        private void Start()
        {
            cam = GetComponent<Camera>();
            doubleClickTime = 0F;
        }

        // Input Control, listens for key input, mouse/touch input and switches EditorModes / selects objects / adds vertices
        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                if (cam.orthographicSize + keyboardZoomStep <= maxSize)
                    cam.orthographicSize += keyboardZoomStep;
            }
            else if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                if (cam.orthographicSize - keyboardZoomStep >= minSize)
                    cam.orthographicSize -= keyboardZoomStep;
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                if (!UIObjectPreferences.menuOpen)
                {
                    if (LevelEditor.TryTestLevel())
                    {
                        SoundManager.ButtonClicked();
                        // animations
                    }
                }
            }
#endif
#if UNITY_ANDROID
            // No items or verticies get curretly dragged and no menu is open, thus listen for input
            if (!vertexDragged && !UIObjectPreferences.menuOpen)
            {
                // If there are two touches on the device manage the editor view (zooming/moving)
                if (Input.touchCount == 2 && UILevelEditor._instance.inventoryScrollRect.velocity.x == 0)
                {
                    // Store both touches.
                    Touch touchZero = Input.GetTouch(0);
                    Touch touchOne = Input.GetTouch(1);

                    // Find the position in the previous frame of each touch.
                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                    // Find the magnitude of the vector (the distance) between the touches in each frame.
                    float prevTouchMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    float touchMag = (touchZero.position - touchOne.position).magnitude;
                    float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                    // Find the difference in the distances between each frame.
                    float deltaMagnitudeDiff = prevTouchMag - touchMag;

                    // fingers moving fast towards each other or fast away from each other => zooming
                    if (touchMag + panThreshold < prevTouchMag || touchMag > prevTouchMag + panThreshold)
                    {
                        // Change the orthographic size based on the change in distance between the touches.
                        float sizeToChange = deltaMagnitudeDiff * zoomSpeed;
                        if ((sizeToChange > 0 && (cam.orthographicSize + sizeToChange) < maxSize) || (sizeToChange < 0 && (cam.orthographicSize + sizeToChange) > minSize))
                        {
                            cam.orthographicSize += sizeToChange;
                        }
                    }
                    // panning => move the camera
                    else
                    {
                        Vector3 touchDeltaPosition = new Vector3(-touchZero.deltaPosition.x * Time.deltaTime * 500, -touchZero.deltaPosition.y * Time.deltaTime * 500, 0);
                        transform.position += touchDeltaPosition;
                        //transform.Translate(touchDeltaPosition.x * Time.deltaTime, touchDeltaPosition.y * Time.deltaTime, 0);
                    }
                }
                // only one finger touches the screen, control object selecting/vertex adding
                else if (Input.touchCount == 1)
                {
                    Touch touch = Input.GetTouch(0);

                    // touch click = > handle selection/ vertex adding
                    if (touch.phase == TouchPhase.Began)
                    {
                        Vector3 position = Camera.main.ScreenToWorldPoint(touch.position);

                        LevelObject l = GetLevelObjectAt(position);
                        if (l != null && l == LevelEditor.selectedObject && LevelEditor.selectedObject.objectType != LevelObject.ObjectType.moveArea)
                        {
                            objectDragged = true;
                        }

                        ClickHandler(position);
                    }

                    // touch drag => handle object movement
                    else if (touch.phase == TouchPhase.Moved)
                    {
                        // an object is selected, and it is not the movearea
                        if (objectDragged && LevelEditor.selectedObject != null && LevelEditor.selectedObject.objectType != LevelObject.ObjectType.moveArea && UILevelEditor._instance.inventoryScrollRect.velocity.x == 0)
                        {
                            Vector3 position = Camera.main.ScreenToWorldPoint(touch.position);
                            //position.y = Screen.height - position.y;
                            position.z = LevelEditor.selectedObject.transform.position.z;
                            if (VertHelper.IsInsideMesh(LevelPlacer.generatedLevel.moveArea.meshFilter.mesh, Vector3.zero, LevelPlacer.generatedLevel.moveArea.transform.InverseTransformPoint(position)))
                                LevelEditor.selectedObject.transform.position = position;
                        }
                    }

                    // touch drag end => snap the dragged object and save the changes that were made
                    else if (touch.phase == TouchPhase.Ended)
                    {
                        // an object is selected, and it is not the movearea
                        if (objectDragged && LevelEditor.selectedObject != null && LevelEditor.selectedObject.objectType != LevelObject.ObjectType.moveArea)
                        {
                            // move the selected object
                            Vector3 position = LevelEditor.selectedObject.transform.position;
                            position = VertHelper.Snap(position, false);
                            if (LevelEditor.selectedObject.transform.position != position)
                            {
                                LevelEditor.selectedObject.transform.position = position;
                                UndoManager.AddUndoPoint();
                            }
                            //itemDragged = false;
                        }
                        objectDragged = false;
                    }
                }
#if UNITY_EDITOR
                // both mouse buttons are held down => drag the editor view
                else if (Input.GetMouseButton(0) && Input.GetMouseButton(1) && UILevelEditor._instance.inventoryScrollRect.velocity.x == 0)
                {
                    Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector3 deltaPos = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 40f;
                    deltaPos.z = 0F;
                    transform.position += deltaPos;
                }
                // one mouse button got clicked => handle selection/vertex adding
                else if (Input.GetMouseButtonDown(0))
                {
                    Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    ClickHandler(position);
                }
                // one mouse button is held down (aka "dragged") => try to move selected objects
                else if (Input.GetMouseButton(0) && UILevelEditor._instance.inventoryScrollRect.velocity.x == 0)
                {
                    // an object is selected, and it is not the movearea
                    if (LevelEditor.selectedObject != null && LevelEditor.selectedObject.objectType != LevelObject.ObjectType.moveArea)
                    {
                        //itemDragged = true;
                        // move the selected object
                        Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        Vector3 deltaPos = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 40f;
                        deltaPos.z = 0F;

                        Vector3 newPos = LevelEditor.selectedObject.transform.position + deltaPos;
                        if (VertHelper.IsInsideMesh(LevelPlacer.generatedLevel.moveArea.meshFilter.mesh, Vector3.zero, LevelPlacer.generatedLevel.moveArea.transform.InverseTransformPoint(newPos)))
                            LevelEditor.selectedObject.transform.position = newPos;
                    }
                }
                // a mouse drag has ended, snap the dragged object and save the changes that were made
                else if (Input.GetMouseButtonUp(0))
                {
                    // an object is selected, and it is not the movearea
                    if (LevelEditor.selectedObject != null && LevelEditor.selectedObject.objectType != LevelObject.ObjectType.moveArea)
                    {
                        // move the selected object
                        Vector3 position = LevelEditor.selectedObject.transform.position;
                        position = VertHelper.Snap(position, false);
                        if (LevelEditor.selectedObject.transform.position != position)
                        {
                            LevelEditor.selectedObject.transform.position = position;
                            UndoManager.AddUndoPoint();
                        }

                        //itemDragged = false;
                    }
                }
#endif
            }

            // lets handle special preference input when a menu is open (yaaay)
            else if (UIObjectPreferences.menuOpen)
            {
                if (UIObjectPreferences.openedMenuType == LevelObject.ObjectType.portal)
                {
                    if (Input.touchCount == 1)
                    {
                        Touch touch = Input.GetTouch(0);
                        // touch click = > handle selection/ vertex adding
                        if (touch.phase == TouchPhase.Began)
                        {
                            Vector3 position = Camera.main.ScreenToWorldPoint(touch.position);
                            ClickHandler(position);
                        }
                    }
                    else if (Input.GetMouseButtonDown(0))
                    {
                        Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        ClickHandler(position);
                    }
                }
            }
        }

        // fire a raycast at the given worldposition and get the levelobject at that postion
        private LevelObject GetLevelObjectAt(Vector3 position)
        {
            LevelObject levelObject = null;

            RaycastHit2D[] hits = Physics2D.RaycastAll(position, Vector2.zero);
            Debug.DrawRay(position, Vector3.forward * 200, Color.green, 20F, false);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null)
                {
                    if (hit.transform.gameObject.layer == LayerMask.NameToLayer("EditorSelectionColliders"))
                    {
                        levelObject = hit.transform.parent.gameObject.GetComponent<LevelObject>();
                    }
                }
            }
            return levelObject;
        }

        // checks for double clicks and selectes/deselects objects accordingly
        private void ClickHandler(Vector3 position)
        {
            bool objectSelected = false;

            if (LevelEditor.editorMode == LevelEditor.EditorMode.select)
            {
                // double click
                if (Time.time - doubleClickTime < doubleClickDelay && doubleClickTime > 0)
                {
                    LevelObject levelObject = GetLevelObjectAt(position);
                    if (levelObject != null)
                    {
                        Debug.Log("SELECTED OBJ " + levelObject.name);
                        LevelEditor.SetSelectedObject(levelObject);
                        objectSelected = true;
                    }

                    // the raycast didn't select any levelobjects
                    if (!objectSelected)
                    {
                        Vector3 localPos = LevelPlacer.generatedLevel.moveArea.transform.InverseTransformPoint(position);
                        bool clickInsideMesh = VertHelper.IsInsideMesh(LevelPlacer.generatedLevel.moveArea.meshFilter.mesh, Vector3.zero, localPos);
                        // we double clicked inside the mesh => select the mesh
                        if (clickInsideMesh)
                        {
                            LevelEditor.SetSelectedObject(LevelPlacer.generatedLevel.moveArea);
                            objectSelected = true;
                        }
                    }
                }
            }
            else if (LevelEditor.editorMode == LevelEditor.EditorMode.edit)
            {
                if (Time.time - doubleClickTime < doubleClickDelay && doubleClickTime > 0)
                {
                    LevelObject levelObject = GetLevelObjectAt(position);
                    if (levelObject != null)
                    {
                        // same object got selected again, deselect it
                        if (levelObject == LevelEditor.selectedObject)
                        {
                            LevelEditor.SetSelectedObject(null);
                            objectSelected = true;
                        }
                        // a new object gets selected, replace the old selection with the new one.
                        else if (levelObject != LevelEditor.selectedObject)
                        {
                            LevelEditor.SetSelectedObject(levelObject);
                            objectSelected = true;
                        }
                    }
                    // the raycast didn't select any levelobjects
                    if (!objectSelected)
                    {
                        Vector3 localPos = LevelPlacer.generatedLevel.moveArea.transform.InverseTransformPoint(position);
                        bool clickInsideMesh = VertHelper.IsInsideMesh(LevelPlacer.generatedLevel.moveArea.meshFilter.mesh, Vector3.zero, localPos);
                        // we double clicked inside the mesh => select the mesh
                        if (clickInsideMesh)
                        {
                            if (LevelEditor.selectedObject.objectType != LevelObject.ObjectType.moveArea)
                                LevelEditor.SetSelectedObject(LevelPlacer.generatedLevel.moveArea);
                            else
                                LevelEditor.SetSelectedObject(null);
                            objectSelected = true;
                        }
                    }
                }

                Debug.Log(LevelEditor.selectedObject);

                // there was no double click detected, if a movearea is selected, try to add vertices
                if (LevelEditor.selectedObject != null && LevelEditor.selectedObject.objectType == LevelObject.ObjectType.moveArea && !Handle.vertGettingSelected)
                {
                    Vector3 screenPosition = Camera.main.WorldToScreenPoint(position);
                    bool isInIgnoreArea = screenPosition.y < Camera.main.pixelHeight * ignoreBottomScreenPercent;
                    if (!isInIgnoreArea)
                        if (VertHandler._instance.VertexAdd(position))
                        {
                            SoundManager.ButtonClicked();
                        }
                }
            }
            else if (LevelEditor.editorMode == LevelEditor.EditorMode.place)
            {
                // snapping
                Vector3 snapPos = VertHelper.Snap(position, false);
                LevelObject.ObjectType objectType = UILevelObject.currentSelectedObject.objectType;

                Vector3 screenPosition = Camera.main.WorldToScreenPoint(snapPos);
                bool isInIgnoreArea = screenPosition.y < Camera.main.pixelHeight * ignoreBottomScreenPercent;

                // the snapping found a valid snap position around the given position
                if (snapPos != Vector3.zero && !isInIgnoreArea)
                {
                    Debug.Log("Buttom screen percent " + ignoreBottomScreenPercent + " screenPos.y " + screenPosition.y);
                    // return to selection mode and deselect the inventory item
                    LevelEditor.SetSelectedObject(LevelPlacer.generatedLevel.AddObject(objectType, snapPos));
                    UILevelObject.onItemSelect.Invoke(null);
                }
            }
            else if (LevelEditor.editorMode == LevelEditor.EditorMode.portalLink)
            {
                if (Time.time - doubleClickTime < doubleClickDelay && doubleClickTime > 0)
                {
                    LevelObject levelObject = GetLevelObjectAt(position);
                    if (levelObject != null)
                    {
                        if (levelObject.objectType == LevelObject.ObjectType.portal)
                        {
                            Portal p = levelObject.GetComponent<Portal>();
                            UIPortalMenu.SelectLinkPortal(p);
                        }
                    }
                }
            }
            doubleClickTime = Time.time;
        }

#endif
    }
}