using System.Threading.Channels;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace POCMultithreadedEngine;

public abstract class RenderThreadMessage {}

public static class RenderThread {
	public static Thread? Thread;

	public static Channel<RenderThreadMessage> Channel =
		System.Threading.Channels.Channel.CreateUnbounded<RenderThreadMessage>(new UnboundedChannelOptions {SingleReader = true});

	public static bool RunLoop = true;
	
	public static void Run() {
		Console.WriteLine("Starting render thread!");
		
		ChannelReader<RenderThreadMessage> channelReader = Channel.Reader;

		//Makes the GL context current to this thread
		Program.Window.MakeCurrent();
		
		GL gl = Program.Window.CreateOpenGL();
		
		gl.ClearColor(1f, 0f, 0f, 1f);

		while (RunLoop) {
			bool peek = channelReader.TryPeek(out _);

			if (peek) {
				if (channelReader.TryRead(out RenderThreadMessage? message)) {
					Console.WriteLine($"Got message on render thread! type:{message.GetType().Name}");
				}
			}

			gl.Clear(ClearBufferMask.ColorBufferBit); 
			
			Program.Window.SwapBuffers();
		}
		
		Console.WriteLine("Render thread stopping!");
	}
}
