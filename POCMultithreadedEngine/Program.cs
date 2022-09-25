using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace POCMultithreadedEngine; 

public static class Program {
	public static  IWindow       Window = null!;
	public static  IInputContext Input  = null!;

	public static void Main(string[] args) {
		WindowOptions windowOptions = WindowOptions.Default with {
			API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(4, 5)),
			Size = new Vector2D<int>(800, 600),
			IsContextControlDisabled = true,
			ShouldSwapAutomatically = false
		};

		Window = Silk.NET.Windowing.Window.Create(windowOptions);
		
		Window.Load += OnLoad;
		Window.Closing += OnClosing;
		
		Window.FramebufferResize += FramebufferResize;
		
		Window.Run();
	}
	
	private static void FramebufferResize(Vector2D<int> obj) {
		RenderThread.Channel.Writer.TryWrite(new ViewportChangeRenderThreadMessage((uint)obj.X, (uint)obj.Y));
	}

	private static void OnClosing() {
		RenderThread.RunLoop = false;
		RenderThread.Thread!.Join();
	}

	private static void OnLoad() {
		Input = Window.CreateInput();

		RenderThread.Thread = new Thread(RenderThread.Run);

		RenderThread.Thread.Start();
	}
}