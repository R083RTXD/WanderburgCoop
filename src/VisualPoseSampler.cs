using UnityEngine;

namespace WanderburgCoopTest;

// Main-thread only. Cache the rig on appearance changes; sample plain numeric
// poses after gameplay updates. The worker never receives a Unity reference.
internal sealed class VisualPoseSampler
{
    private VP source;
    private uint appearance;
    private List<VehicleModel.PoseSource> nodes;
    private bool limitLogged;
    private bool originLogged;
    private readonly HashSet<string> loggedStates = new();
    private static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Capture");
    public string Status { get; private set; } = "Callback ausstehend";
    private void Report(string status, string detail = null)
    {
        Status = status;
        // Bounded state diagnostics, never full payloads or session tokens.
        if (loggedStates.Count < 10 && loggedStates.Add(status))
            Log.LogInfo($"PID {Environment.ProcessId}: capture={status}; {detail}");
    }

    public VisualPoseFrame Capture(VP vehicle, PlayerSnapshot snapshot)
    {
        if (!vehicle || !snapshot.InRun || snapshot.AppearanceId == 0)
        {
            Report(!vehicle ? "kein Fahrzeug" : !snapshot.InRun ? "kein Run" : "keine Modellkennung");
            source = null; nodes = null;
            return null;
        }
        if (source != vehicle || appearance != snapshot.AppearanceId || nodes == null)
        {
            source = vehicle;
            appearance = snapshot.AppearanceId;
            nodes = VehicleModel.GetPoseSources(vehicle);
            limitLogged = false;
        }
        if (nodes.Count > VisualPoseFrame.MaxNodes)
        {
            Report("zu viele Knoten", nodes.Count.ToString());
            if (!limitLogged)
                BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Animation").LogWarning(
                    $"Rig has {nodes.Count} nodes; animation skipped above {VisualPoseFrame.MaxNodes} nodes.");
            limitLogged = true;
            return null;
        }
        var scale = vehicle.transform.lossyScale;
        if (Math.Abs(scale.x) < 0.000001f || Math.Abs(scale.y) < 0.000001f || Math.Abs(scale.z) < 0.000001f)
        {
            Report("Fahrzeugskalierung null");
            return null;
        }
        var poses = new VisualNodePose[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (!node.Transform) { nodes = null; Report("Knoten verschwunden"); return null; }
            var t = node.Transform;
            if (node.OwnerFrame)
            {
                var worldPosition = t.position;
                var worldRotation = t.rotation;
                poses[i] = VisualRootPose.Capture(snapshot,
                    new System.Numerics.Vector3(worldPosition.x, worldPosition.y, worldPosition.z),
                    new System.Numerics.Quaternion(worldRotation.x, worldRotation.y, worldRotation.z, worldRotation.w),
                    new System.Numerics.Vector3(scale.x, scale.y, scale.z));
                if (!originLogged)
                {
                    originLogged = true;
                    Log.LogInfo($"PID {Environment.ProcessId}: visual root world={worldPosition}, rotation={t.eulerAngles}, scale={scale}, snapshot=({snapshot.X},{snapshot.Y},{snapshot.Z}) yaw={snapshot.Yaw}");
                }
                continue;
            }
            Vector3 contactPosition = t.position;
            if (node.Soles != null && node.Soles.Length > 0)
            {
                var sole = new SoleHeight();
                foreach (var renderer in node.Soles)
                    if (renderer && renderer.enabled) sole.Include(renderer.bounds.min.y);
                contactPosition.y = sole.OrFallback(contactPosition.y);
            }
            Vector3 p = node.VehicleRelative ? vehicle.transform.InverseTransformPoint(contactPosition) : t.localPosition;
            Quaternion q = node.VehicleRelative ? Quaternion.Inverse(vehicle.transform.rotation) * t.rotation : t.localRotation;
            Vector3 s = node.VehicleRelative ? new Vector3(t.lossyScale.x / scale.x,
                t.lossyScale.y / scale.y, t.lossyScale.z / scale.z) : t.localScale;
            poses[i] = new VisualNodePose(node.Id, p.x, p.y, p.z, q.x, q.y, q.z, q.w, s.x, s.y, s.z);
            if (node.Id == VehicleIdentity.FromGroundContact(0, true) && !loggedStates.Contains("source-contact"))
            {
                loggedStates.Add("source-contact");
                Log.LogInfo($"PID {Environment.ProcessId}: native foot target={t.name}, world={t.position}, sole-contact={contactPosition}, soleRenderers={node.Soles?.Length ?? 0}, vehicle-relative={p}");
            }
        }
        var frame = new VisualPoseFrame(snapshot.Scene, snapshot.VehicleId, snapshot.AppearanceId, poses);
        Report(frame.IsValid ? $"bereit: {poses.Length} Knoten" : "Pose ungueltig", frame.InvalidReason);
        return frame.IsValid ? frame : null;
    }
}
