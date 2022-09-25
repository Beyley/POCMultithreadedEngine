using Silk.NET.OpenGL;

namespace POCMultithreadedEngine; 

public class Texture {
	private readonly GL  gl;
	private readonly uint _width;
	private readonly uint _height;

	public readonly uint Id;
	public unsafe Texture(GL gl, int width, int height) {
		this.gl      = gl;
		this._width  = (uint)width;
		this._height = (uint)height;
		this.Id      = gl.GenTexture();

		this.Bind();
		gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
		this.Unbind();
	}

	public void SetData<T>(ReadOnlySpan<T> data) where T : unmanaged {
		this.Bind();
		this.gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, this._width, this._height, PixelFormat.Rgba, PixelType.UnsignedByte, data);
		this.Unbind();
	}

	public void Bind() {
		this.gl.BindTexture(TextureTarget.Texture2D, this.Id);

	}

	public void Unbind() {
		this.gl.BindTexture(TextureTarget.Texture2D, 0);
	}
}
