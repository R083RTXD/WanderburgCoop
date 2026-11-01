using UnityEngine;
using Object = UnityEngine.Object;

namespace WanderburgCoopTest;

// Local HUD only, using existing position snapshots. No game item registration,
// world objects, save state or extra network messages.
internal sealed class PlayerEdgeIndicator : IDisposable
{
    private Texture2D shape;
    private bool failed;
    private static readonly BepInEx.Logging.ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("Wanderburg Co-op Edge Marker");
    public void Draw(ConnectionInfo connection, PlayerSnapshot local)
    {
        if (failed) return;
        try { DrawCore(connection, local); }
        catch (Exception ex) { failed = true; Log.LogError($"Teammate edge marker disabled; other mod functions retained: {ex}"); }
    }
    private void DrawCore(ConnectionInfo connection, PlayerSnapshot local)
    {
        if (Event.current.type != EventType.Repaint ||
            !PlayerEdgePosition.Eligible(connection.Connected, connection.HasFreshSnapshot, local, connection.RemoteSnapshot)) return;
        var camera = Camera.main;
        if (!camera) return;
        var remote = connection.RemoteSnapshot;
        var point = camera.WorldToViewportPoint(new Vector3(remote.X, remote.Y + 2, remote.Z));
        if (!PlayerEdgePosition.TryPlace(point.x, point.y, point.z, Screen.width, Screen.height, out float x, out float y, out float angle)) return;
        EnsureGraphics();
        var oldColor = GUI.color;
        var oldMatrix = GUI.matrix;
        try
        {
            GUI.color = Color.white;
            GUIUtility.RotateAroundPivot(angle, new Vector2(x,y));
            GUI.DrawTexture(new Rect(x-32,y-32,64,64),shape);
            GUI.matrix = oldMatrix;
            // Castle stays upright while the surrounding pointer rotates.
            GUI.color = new Color(0.1f,0.9f,1,1);
            Block(x-10,y-5,20,14); Block(x-12,y-10,6,19); Block(x+6,y-10,6,19);
            Block(x-12,y-14,4,5); Block(x-4,y-10,4,5); Block(x+8,y-14,4,5);
            GUI.color = new Color(0.12f,0.13f,0.14f,1);
            Block(x-2,y+2,4,7);
            GUI.color = Color.white;
            float labelX = Mathf.Clamp(x-32,4,Screen.width-84);
            float labelY = y > Screen.height-80 ? y-43 : y+20;
            GUI.Label(new Rect(labelX,labelY,80,20),"Mitspieler");
        }
        finally { GUI.color = oldColor; GUI.matrix = oldMatrix; }
    }
    private static void Block(float x,float y,float w,float h) => GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);
    private void EnsureGraphics()
    {
        if (shape) return;
        shape = new Texture2D(64,64,TextureFormat.RGBA32,false);
        var pixels = new Color32[64*64];
        for (int y=0;y<64;y++) for(int x=0;x<64;x++)
        {
            int dx=x-32,dy=y-32;
            bool circle=dx*dx+dy*dy<=18*18;
            bool tip=dx>=13 && dx<=29 && Math.Abs(dy)<=(29-dx)*0.65;
            pixels[y*64+x]=circle||tip ? new Color32(31,33,36,250) : new Color32(0,0,0,0);
        }
        shape.SetPixels32(pixels); shape.Apply();
    }
    public void Dispose() { if(shape) Object.Destroy(shape); shape=null; }
}
