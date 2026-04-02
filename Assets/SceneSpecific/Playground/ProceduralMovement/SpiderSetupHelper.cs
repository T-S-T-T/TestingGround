#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// SpiderSetupHelper — Editor-only utility.
/// Select your Spider root in the Hierarchy and open:
///   Tools > Spider > Auto-Create Rest Positions
/// This creates the 8 RestPosition GameObjects in the correct layout
/// so you do not have to position them by hand.
/// </summary>
public class SpiderSetupHelper : MonoBehaviour
{
    // This component is never added manually —
    // the menu item below does all the work.
}

public static class SpiderSetupMenu
{
    /// <summary>
    /// Layout of rest positions relative to the spider body centre.
    /// X = left(-)/right(+),  Y = down (terrain offset),  Z = front(+)/rear(-)
    /// Tune these to match your spider mesh proportions.
    /// </summary>
    static readonly (string name, Vector3 localPos)[] RestLayout = new[]
    {
        // Front pair
        ("RestPos_FL", new Vector3(-0.55f, -0.30f,  0.70f)),
        ("RestPos_FR", new Vector3( 0.55f, -0.30f,  0.70f)),
        // Mid pair
        ("RestPos_ML", new Vector3(-0.70f, -0.30f,  0.10f)),
        ("RestPos_MR", new Vector3( 0.70f, -0.30f,  0.10f)),
        // Rear-mid pair
        ("RestPos_RL", new Vector3(-0.70f, -0.30f, -0.30f)),
        ("RestPos_RR", new Vector3( 0.70f, -0.30f, -0.30f)),
        // Back pair
        ("RestPos_BL", new Vector3(-0.55f, -0.30f, -0.70f)),
        ("RestPos_BR", new Vector3( 0.55f, -0.30f, -0.70f)),
    };

    [MenuItem("Tools/Spider/Auto-Create Rest Positions")]
    static void CreateRestPositions()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Spider Setup",
                "Select your Spider root GameObject first.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Auto-Create Spider Rest Positions");
        int group = Undo.GetCurrentGroup();

        // Create a parent to keep the Hierarchy tidy
        GameObject restParent = new GameObject("RestPositions");
        Undo.RegisterCreatedObjectUndo(restParent, "Create RestPositions parent");
        restParent.transform.SetParent(selected.transform, false);

        foreach (var (restName, localPos) in RestLayout)
        {
            GameObject go = new GameObject(restName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {restName}");
            go.transform.SetParent(restParent.transform, false);
            go.transform.localPosition = localPos;
        }

        Undo.CollapseUndoOperations(group);

        Debug.Log($"[SpiderSetup] Created {RestLayout.Length} rest positions " +
                  $"under '{selected.name}/RestPositions'.\n" +
                  "Assign each RestPos_XX to the matching LegStepController.restPosition field.");

        // Select the new parent so the user can see it immediately
        Selection.activeGameObject = restParent;
        EditorGUIUtility.PingObject(restParent);
    }

    [MenuItem("Tools/Spider/Auto-Create Rest Positions", validate = true)]
    static bool ValidateCreateRestPositions()
        => Selection.activeGameObject != null;
}
#endif
