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
		
		Window.Run();
	}
	
	private static void OnClosing() {
		RenderThread.RunLoop = false;
	}

	private static void OnLoad() {
		Input = Window.CreateInput();

		RenderThread.Thread = new Thread(RenderThread.Run);

		RenderThread.Thread.Start();
	}
}