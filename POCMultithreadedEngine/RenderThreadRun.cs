using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Buffer = Silk.NET.OpenGL.Buffer;

namespace POCMultithreadedEngine;

public abstract class RenderThreadMessage {}

public class ViewportChangeRenderThreadMessage : RenderThreadMessage {
	public uint Width;
	public uint Height;

	public ViewportChangeRenderThreadMessage(uint width, uint height) {
		this.Width  = width;
		this.Height = height;
	}
}

public static class RenderThread {
	public static Thread? Thread;

	public static Channel<RenderThreadMessage> Channel =
		System.Threading.Channels.Channel.CreateUnbounded<RenderThreadMessage>(new UnboundedChannelOptions {SingleReader = true});

	public static bool RunLoop = true;
	
	private static Vector2D<uint> _viewport;

	public static unsafe void Run() {
		Console.WriteLine("Starting render thread!");

		ChannelReader<RenderThreadMessage> channelReader = Channel.Reader;

		//Makes the GL context current to this thread
		Program.Window.MakeCurrent();

		GL gl = Program.Window.CreateOpenGL();

		gl.Enable(EnableCap.DebugOutput);
		gl.Enable(EnableCap.DebugOutputSynchronous);
		
		gl.Enable(EnableCap.Blend);
		gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

		gl.DebugMessageCallback(DebugCallback, null);

		gl.ClearColor(0f, 0f, 0f, 1f);

		Buffer vtxBuf = new(gl.CreateBuffer());

		VertexArray vao = new(gl.GenVertexArray());
		gl.BindVertexArray(vao.Handle);
		gl.BindBuffer(BufferTargetARB.ArrayBuffer, vtxBuf.Handle);

		gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.Position)));
		gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex),
			(void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.TextureCoordinate)));
		gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.Color)));
		gl.VertexAttribIPointer(3, 1, VertexAttribIType.Int, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.TextureId)));

		gl.EnableVertexAttribArray(0);
		gl.EnableVertexAttribArray(1);
		gl.EnableVertexAttribArray(2);
		gl.EnableVertexAttribArray(3);

		gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
		gl.BindVertexArray(0);

		Vertex[] vertexData = {
			new Vertex {
				Color             = new Color(1, 0, 0, 1),
				Position          = new Vector2(100),
				TextureCoordinate = new Vector2(0, 0),
				TextureId         = 0
			},
			new Vertex {
				Color             = new Color(0, 1, 0, 1),
				Position          = new Vector2(200, 100),
				TextureCoordinate = new Vector2(1, 0),
				TextureId         = 0
			},
			new Vertex {
				Color             = new Color(0, 0, 1, 1),
				Position          = new Vector2(100, 200),
				TextureCoordinate = new Vector2(0, 1),
				TextureId         = 0
			},
			new Vertex {
				Color             = new Color(1, 1, 1, 0),
				Position          = new Vector2(200),
				TextureCoordinate = new Vector2(1, 1),
				TextureId         = 0
			}
		};

		gl.NamedBufferData<Vertex>(vtxBuf.Handle, (nuint)(sizeof(Vertex) * vertexData.Length), vertexData, VertexBufferObjectUsage.StaticDraw);

		uint program = Shaders.GetShader(gl);

		Texture tex = new(gl, 1, 1);

		Rgba32[] pix = {new Rgba32(255, 255, 255, 255)};
		tex.SetData<Rgba32>(pix);

		_viewport = new Vector2D<uint>((uint)Program.Window.Size.X, (uint)Program.Window.Size.Y);
		UpdateProjectionMatrix(gl, program);

		while (RunLoop) {
			bool peek = channelReader.TryPeek(out _);

			if (peek) {
				if (channelReader.TryRead(out RenderThreadMessage? message)) {
					switch (message) {
						case ViewportChangeRenderThreadMessage viewportMessage:
							gl.Viewport(0, 0, viewportMessage.Width, viewportMessage.Height);
							_viewport = new Vector2D<uint>(viewportMessage.Width, viewportMessage.Height);
							UpdateProjectionMatrix(gl, program);
							break;
					}
				}
			}

			//Clear the screen
			gl.Clear(ClearBufferMask.ColorBufferBit);

			//Bind the VAO
			gl.BindVertexArray(vao.Handle);

			gl.UseProgram(program);

			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vtxBuf.Handle);
			tex.Bind();

			gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

			//Unbind the VAO
			gl.BindVertexArray(0);

			Program.Window.SwapBuffers();
		}

		Console.WriteLine("Render thread stopping!");
	}

	private static unsafe void UpdateProjectionMatrix(GL gl, uint program) {
		Matrix4x4 mat = Matrix4x4.CreateOrthographicOffCenter(0, _viewport.X, _viewport.Y, 0, 0, 1);

		int uniformLocation = gl.GetUniformLocation(program, "ProjectionMatrix");
		gl.ProgramUniformMatrix4(program, uniformLocation, 1, false, (float*)&mat);
	}
	
	private static void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam) {
		Console.WriteLine(SilkMarshal.PtrToString(message));
	}
}
