using System.Numerics;
using System.Runtime.InteropServices;

namespace POCMultithreadedEngine; 

[StructLayout(LayoutKind.Sequential)]
public struct Vertex {
	public Vector2 Position;
	public Vector2 TextureCoordinate;
	public Color   Color;
	public int    TextureId;
}
