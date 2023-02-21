using UnityEngine;

namespace TextureCombine
{
    public static class Style
    {
        public static readonly GUIStyle TitleStyle = new GUIStyle
        {
            normal =
            {
                textColor = Color.white
            },
            fontSize = 20
        };
        public static readonly GUIStyle H2Style = new GUIStyle
        {
            normal =
            {
                textColor = Color.white
            },
            fontSize = 16
        };
        public static readonly GUIStyle H3Style = new GUIStyle
        {
            normal =
            {
                textColor = new Color(0.9f,0.9f,0.9f),
            },
            fontSize = 13
        };
    }
}