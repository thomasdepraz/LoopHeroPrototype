using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Presets;
using Object = UnityEngine.Object;

[Serializable]
public class ToolWindow : EditorWindow
{
    Object[] targets = new Object[1];
    GUIStyle labelStyle;


    [MenuItem("Tools/ToolWindow &t")]
    static void OpenWindow()
    {

        if(!EditorWindow.HasOpenInstances<ToolWindow>())
        {
            ToolWindow window = (ToolWindow)GetWindow(typeof(ToolWindow));

            window.titleContent = new GUIContent("Tool Window");
            window.minSize = new Vector2(200, 200);
            window.maxSize = new Vector2(200, 200);

            window.Show();
            window.Focus();
        }
        else
        {
            EditorWindow.FocusWindowIfItsOpen(typeof(ToolWindow));
        }
    }



    private void CreateGUI()
    {
        labelStyle = new GUIStyle();
        labelStyle.padding.left = 10;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleLeft;

        VisualElement root = rootVisualElement;
        root.StretchToParentSize();

        Action GUIHandler = HandleGUI;

        IMGUIContainer container = new IMGUIContainer();
        container.onGUIHandler = GUIHandler;
        
        targets[0] = this;
       
        root.Add(container);
    }


    private void HandleGUI()
    {
        Repaint();
        float rootWidth = rootVisualElement.layout.width;
        Vector2 rootPos = rootVisualElement.transform.position;

        GUI.Box(new Rect(rootPos, new Vector2(rootWidth, 20)), new GUIContent(""));
        GUI.Label(new Rect(rootPos, new Vector2(rootWidth,20)), new GUIContent("Preset"), labelStyle);
        PresetSelector.DrawPresetButton(new Rect(new Vector2(rootWidth - 20, 3), new Vector2(1, 1)), targets); 
    }



    private void OnGUI()
    {

       
    }
}
