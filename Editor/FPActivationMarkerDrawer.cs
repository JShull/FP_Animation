namespace FuzzPhyte.Utility.Animation.Editor
{
    using FuzzPhyte.Utility.Animation.Timeline;
    using UnityEditor;
    using UnityEditor.Timeline;
    using UnityEngine;
    using UnityEngine.Timeline;
    [CustomTimelineEditor(typeof(FPActivationMarker))]
    public class FPActivationMarkerDrawer:MarkerEditor
    {
        public float PadSize = 0.025f;
        public float PadScale = 0.75f;
        // --- Colors per command ---
        static Color ColorFor(FPActivationCommand cmd) => cmd switch
        {
            FPActivationCommand.Activate => new Color(0.01f, 0.9f, .25f),
            FPActivationCommand.Deactivate => new Color(1f, 0.25f, 0),
            _ => new Color(0.5f, 0.5f, 0.5f)
        };
        // --- Different built-in icons per command ---
        static Texture2D IconFor(FPActivationCommand cmd)
        {
            // Uses Unity's built-in icon names; swap to your own textures if you prefer.
            // (The "d_" variants pick dark-skin versions.)
            string name = cmd switch
            {
                FPActivationCommand.Activate => "d_Animation.AddKeyframe",
                FPActivationCommand.Deactivate => "d_Animation.AddKeyframe",
                _ => null
            };
            // Prefer your primary mapping, otherwise try reasonable generic fallbacks.
            return TryGetIcon(
                name,
                // generic fallbacks (pick any you like)
                EditorGUIUtility.isProSkin ? "d_PlayButton On" : "PlayButton On",
                "PlayButton On",
                "console.infoicon",
                "UnityEditor.AnimationWindow" // last-ditch; may vary by version
            );
            //return (Texture2D)EditorGUIUtility.IconContent(name).image;
        }
        static Texture2D TryGetIcon(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var n = names[i];
                if (string.IsNullOrEmpty(n)) continue;

                // FindTexture is “quiet” compared to IconContent (won’t log errors every draw)
                var tex = EditorGUIUtility.FindTexture(n);
                if (tex != null) return tex;

                // Some icons are only available via IconContent in some versions—still guard it.
                var gc = EditorGUIUtility.IconContent(n);
                if (gc != null && gc.image is Texture2D t2d) return t2d;
            }
            return null;
        }
        // Optional: draw a tiny label on top of the marker
        public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
        {
            if (marker is not FPActivationMarker m) return;

            var r = region.markerRegion;

            // Optional: soften / neutralize the default rectangle background
            EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0f));

            // ---- Triangle sizing ----
            
            float height = Mathf.Max(6f, r.height - PadSize * 2f);
            float width = Mathf.Min(r.width - PadSize * PadScale, height * 1.8f); // WIDER than tall

            float cx = r.x + r.width * 0.5f;
            float topY = r.y + PadSize;
            float botY = topY + height;

            // ---- Downward-pointing triangle ----
            Vector3 p0 = new Vector3(cx - width * PadScale, topY, 0f); // top-left
            Vector3 p1 = new Vector3(cx + width * PadScale, topY, 0f); // top-right
            Vector3 p2 = new Vector3(cx, botY, 0f);                // bottom tip

            Handles.BeginGUI();
            Handles.color = ColorFor(m.Command);
            Handles.DrawAAConvexPolygon(p0, p1, p2);

            // Optional outline for contrast
            Handles.color = new Color(0f, 0f, 0f, 0.35f);
            Handles.DrawAAPolyLine(2f, p0, p1, p2, p0);
            Handles.EndGUI();

        }

        // helpers to keep GUI state sane
        private readonly struct GUIColorScope : System.IDisposable
        {
            private readonly Color prev;
            public GUIColorScope(Color c) { prev = GUI.color; GUI.color = c; }
            public void Dispose() { GUI.color = prev; }
        }
        private readonly struct GUIEnabledScope : System.IDisposable
        {
            private readonly bool prev;
            public GUIEnabledScope(bool enabled) { prev = GUI.enabled; GUI.enabled = enabled; }
            public void Dispose() { GUI.enabled = prev; }
        }
    }
}
