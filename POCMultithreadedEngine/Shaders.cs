using System.Text;
using Silk.NET.OpenGL;

namespace POCMultithreadedEngine; 

public static class Shaders {
	public static uint GetShader(GL gl) {
		uint program = gl.CreateProgram();

		Shader vtxShader = new(gl.CreateShader(ShaderType.VertexShader));
		Shader frgShader = new(gl.CreateShader(ShaderType.FragmentShader));

		const string vtxSrc = @"#version 450

layout(location = 0) in vec2 VertexPosition;
layout(location = 1) in vec2 VertexTextureCoordinate;
layout(location = 2) in vec4 VertexColor;
layout(location = 3) in int TexId;

uniform mat4 ProjectionMatrix;

layout(location = 0) out vec2 FragTextureCoordinate;
layout(location = 1) out vec4 FragVertexColor;
layout(location = 2) out flat int FragTexId;

void main() {
	//Set the position using the projection matrix
	gl_Position = ProjectionMatrix * vec4(VertexPosition, 0, 1);

	//Set the various things that will go into the fragment shader
	FragTextureCoordinate = VertexTextureCoordinate;
	FragVertexColor = VertexColor;
	FragTexId = TexId;
}
";
		
		string frgSrc = @"#version 450

//The inputs from the vertex shader
layout(location = 0) in vec2 FragTextureCoordinate;
layout(location = 1) in vec4 FragVertexColor;
layout(location = 2) in flat int FragTexId;

//The output color
layout(location = 0) out vec4 OutputColor;

$sampler2d_uniforms$

void main() {
	$sample_textures_from_id$
	else //If we were unable to match the id, just output pure red, indicating invalid texture
		OutputColor = vec4(1, 0, 0, 1);
}";

		BuildFragmentShader(gl, ref frgSrc);
		
		gl.ShaderSource(vtxShader.Handle, vtxSrc);
		gl.ShaderSource(frgShader.Handle, frgSrc);
		
		gl.CompileShader(vtxShader.Handle);
		gl.CompileShader(frgShader.Handle);
		
		Console.WriteLine($"Vtx: {gl.GetShaderInfoLog(vtxShader.Handle)}");
		Console.WriteLine($"Frg: {gl.GetShaderInfoLog(frgShader.Handle)}");
		
		gl.AttachShader(program, vtxShader.Handle);
		gl.AttachShader(program, frgShader.Handle);
		
		gl.LinkProgram(program);
		
		Console.WriteLine($"Link: {gl.GetProgramInfoLog(program)}");
		
		gl.DeleteShader(vtxShader.Handle);
		gl.DeleteShader(frgShader.Handle);

		gl.GetInteger(GetPName.MaxTextureImageUnits, out int texUnits);

		for (int i = 0; i < texUnits; i++) {
			int uniformLocation = gl.GetUniformLocation(program, $"Texture{i}");
			gl.ProgramUniform1(program, uniformLocation, i);
		}

		return program;
	}
	private static void BuildFragmentShader(GL gl, ref string frgSrc) {
		StringBuilder builder = new();

		gl.GetInteger(GetPName.MaxTextureImageUnits, out int units);

		//Build the uniforms
		for (int i = 0; i < units; i++) {
			builder.AppendLine($"uniform sampler2D Texture{i};");
		}

		frgSrc = frgSrc.Replace("$sampler2d_uniforms$", builder.ToString());
		
		builder.Clear();

		//Build the texture sampling
		for (int i = 0; i < units; i++) {
			if (i != 0)
				builder.Append("else ");
			
			builder.AppendLine($"if (FragTexId == {i}) OutputColor = texture(Texture{i}, FragTextureCoordinate) * FragVertexColor;");
		}

		frgSrc = frgSrc.Replace("$sample_textures_from_id$", builder.ToString());
	}
}
