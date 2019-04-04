using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Linq;
using System.IO.Compression;

namespace uSrcTools
{

public class VTFLoader : MonoBehaviour 
{
	[System.Serializable]
	public struct vtfheader
	{
		public string signature;		// File signature ("VTF\0"). (or as little-endian integer, 0x00465456)
		public uint[] version;      //2	// version[0].version[1] (currently 7.2).
		public uint	headerSize;			// Size of the header struct (16 byte aligned; currently 80 bytes).
		public short width;			// Width of the largest mipmap in pixels. Must be a power of 2.
		public short height;			// Height of the largest mipmap in pixels. Must be a power of 2.
		public uint	flags;				// VTF flags.
		public ushort frames;			// Number of frames, if animated (1 for no animation).
		public ushort firstFrame;		// First frame in animation (0 based).
		public byte[] padding0;		//4	// reflectivity padding (16 byte alignment).
		public Vector3 reflectivity;//3	// reflectivity vector.
		public byte[] padding1;     //4	// reflectivity padding (8 byte packing).
		public float bumpmapScale;		// Bumpmap scale.
		public VTFImageFormat	highResImageFormat;	// High resolution image format.
		public byte mipmapCount;		// Number of mipmaps.
		public VTFImageFormat lowResImageFormat;	// Low resolution image format (always DXT1).
		public byte lowResImageWidth;	// Low resolution image width.
		public byte	lowResImageHeight;	// Low resolution image height.
		public ushort depth;			// Depth of the largest mipmap in pixels.
										// Must be a power of 2. Can be 0 or 1 for a 2D texture (v7.2 only).
	}

	public struct troikaTextureHeader
	{
		public string signature;
        public ushort numVTF;
	    public ushort flags;
	    public uint VTFSize;
        public char[] Padding2;
	}

    const int IMAGE_FLAG_POINT_SAMPLING = 0x0001;
    const int IMAGE_FLAG_TRILINEAR_SAMPLING = 0x0002;
    const int IMAGE_FLAG_CLAMP_S = 0x0004;
    const int IMAGE_FLAG_CLAMP_T = 0x0008;
    const int IMAGE_FLAG_ANISOTROPIC_SAMPLING = 0x0010;
    const int IMAGE_FLAG_HINT_DXT5 = 0x0020;
    const int IMAGE_FLAG_NO_COMPRESS = 0x0040;
    const int IMAGE_FLAG_NORMAL_MAP = 0x0080;
    const int IMAGE_FLAG_NO_MIPMAPS = 0x0100;
    const int IMAGE_FLAG_NO_LEVEL_OF_DETAIL = 0x0200;
    const int IMAGE_FLAG_NO_MINIMUM_MIPMAP = 0x0400;
    const int IMAGE_FLAG_PROCEDURAL = 0x0800;
    const int IMAGE_FLAG_ONE_BIT_ALPHA = 0x1000;
    const int IMAGE_FLAG_EIGHT_BIT_ALPHA = 0x2000;
    const int IMAGE_FLAG_ENVIRONMENT_MAP = 0x4000;
    const int IMAGE_FLAG_RENDER_TARGET = 0x8000;
    const int IMAGE_FLAG_DEPTH_RENDER_TARGET = 0x10000;
    const int IMAGE_FLAG_NO_DEBUG_OVERRIDE = 0x20000;
    const int IMAGE_FLAG_SINGLE_COPY = 0x40000;
    const int IMAGE_FLAG_PRE_SRGB = 0x80000;
    const int IMAGE_FLAG_PREMULTIPLY_COLOR_BY_ONE_OVER_MIPMAP_LEVEL = 0x100000;
    const int IMAGE_FLAG_NORMAL_TO_DUDV = 0x200000;
    const int IMAGE_FLAG_ALPHA_TEST_MIPMAP_GENERATION = 0x400000;
    const int IMAGE_FLAG_NO_DEPTH_BUFFER = 0x800000;
    const int IMAGE_FLAG_NICE_FILTERED = 0x1000000;
    const int IMAGE_FLAG_CLAMP_U = 0x2000000;
    const int IMAGE_FLAG_VERTEX_TEXTURE = 0x4000000;
    const int IMAGE_FLAG_SSBUMP = 0x8000000;
    const int IMAGE_FLAG_BORDER = 0x20000000;

    public enum VTFImageFormat
	{
		IMAGE_FORMAT_RGBA8888 = 0,				//!<  = Red, Green, Blue, Alpha - 32 bpp
		IMAGE_FORMAT_ABGR8888,					//!<  = Alpha, Blue, Green, Red - 32 bpp
		IMAGE_FORMAT_RGB888,					//!<  = Red, Green, Blue - 24 bpp
		IMAGE_FORMAT_BGR888,					//!<  = Blue, Green, Red - 24 bpp
		IMAGE_FORMAT_RGB565,					//!<  = Red, Green, Blue - 16 bpp
		IMAGE_FORMAT_I8,						//!<  = Luminance - 8 bpp
		IMAGE_FORMAT_IA88,						//!<  = Luminance, Alpha - 16 bpp
		IMAGE_FORMAT_P8,						//!<  = Paletted - 8 bpp
		IMAGE_FORMAT_A8,						//!<  = Alpha- 8 bpp
		IMAGE_FORMAT_RGB888_BLUESCREEN,			//!<  = Red, Green, Blue, "BlueScreen" Alpha - 24 bpp
		IMAGE_FORMAT_BGR888_BLUESCREEN,			//!<  = Red, Green, Blue, "BlueScreen" Alpha - 24 bpp
		IMAGE_FORMAT_ARGB8888,					//!<  = Alpha, Red, Green, Blue - 32 bpp
		IMAGE_FORMAT_BGRA8888,					//!<  = Blue, Green, Red, Alpha - 32 bpp
		IMAGE_FORMAT_DXT1,						//!<  = DXT1 compressed format - 4 bpp
		IMAGE_FORMAT_DXT3,						//!<  = DXT3 compressed format - 8 bpp
		IMAGE_FORMAT_DXT5,						//!<  = DXT5 compressed format - 8 bpp
		IMAGE_FORMAT_BGRX8888,					//!<  = Blue, Green, Red, Unused - 32 bpp
		IMAGE_FORMAT_BGR565,					//!<  = Blue, Green, Red - 16 bpp
		IMAGE_FORMAT_BGRX5551,					//!<  = Blue, Green, Red, Unused - 16 bpp
		IMAGE_FORMAT_BGRA4444,					//!<  = Red, Green, Blue, Alpha - 16 bpp
		IMAGE_FORMAT_DXT1_ONEBITALPHA,			//!<  = DXT1 compressed format with 1-bit alpha - 4 bpp
		IMAGE_FORMAT_BGRA5551,					//!<  = Blue, Green, Red, Alpha - 16 bpp
		IMAGE_FORMAT_UV88,						//!<  = 2 channel format for DuDv/Normal maps - 16 bpp
		IMAGE_FORMAT_UVWQ8888,					//!<  = 4 channel format for DuDv/Normal maps - 32 bpp
		IMAGE_FORMAT_RGBA16161616F,				//!<  = Red, Green, Blue, Alpha - 64 bpp
		IMAGE_FORMAT_RGBA16161616,				//!<  = Red, Green, Blue, Alpha signed with mantissa - 64 bpp
		IMAGE_FORMAT_UVLX8888,					//!<  = 4 channel format for DuDv/Normal maps - 32 bpp
		IMAGE_FORMAT_R32F,						//!<  = Luminance - 32 bpp
		IMAGE_FORMAT_RGB323232F,				//!<  = Red, Green, Blue - 96 bpp
		IMAGE_FORMAT_RGBA32323232F,				//!<  = Red, Green, Blue, Alpha - 128 bpp
		IMAGE_FORMAT_NV_DST16,
		IMAGE_FORMAT_NV_DST24,					
		IMAGE_FORMAT_NV_INTZ,
		IMAGE_FORMAT_NV_RAWZ,
		IMAGE_FORMAT_ATI_DST16,
		IMAGE_FORMAT_ATI_DST24,
		IMAGE_FORMAT_NV_NULL,
		IMAGE_FORMAT_ATI2N,						
		IMAGE_FORMAT_ATI1N,

		IMAGE_FORMAT_COUNT,
		IMAGE_FORMAT_NONE = -1
	}
	
	public bool remipmap;

    public static bool IsTTFileLoad(string name)
    {
        string fullName = "";
        string path = "";

        if(!name.Contains(".tth"))
            fullName = name + ".tth";

        path = ResourceManager.GetPath ("materials/"+fullName);
		if(path==null)
		{
			return false;
		}

        if(!name.Contains(".ttz"))
            fullName = name + ".ttz";

        path = ResourceManager.GetPath ("materials/"+fullName);
		if(path==null)
		{
			return false;
		}

        return true;
    }

    public static Texture2D LoadFile(string name)
	{
		BinaryReader BR;

		string path = "";
        string fullName = "";

        bool isTTFile = IsTTFileLoad(name);
        
        if (!isTTFile) {
		    if(!name.Contains(".vtf"))
			    fullName = name + ".vtf";

		    path = ResourceManager.GetPath ("materials/"+ fullName);
		
		    if(path==null)
		    {
		    //	Debug.LogWarning("materials/"+name+".vtf: Not Found");
			    return null;
		    }

		    BR = new BinaryReader (File.Open (path, FileMode.Open));

            BR.BaseStream.Seek (0, SeekOrigin.Begin);
		    vtfheader header = ReadHeader (BR);

		    byte[] ImageData;
		    uint ImageDataSize;
		    byte[] ThumbnailImageData;
		    uint ThumbnailDataSize;

		    ReadData (BR, header,out ImageData, out ImageDataSize, out ThumbnailImageData, out ThumbnailDataSize);

		    BR.BaseStream.Dispose ();
		    //return CreateThumbnailTexture (name);


		    return CreateTexture (fullName,header,ImageData);
        } else {
		    if(!name.Contains(".tth"))
			    fullName = name + ".tth";

		    path = ResourceManager.GetPath ("materials/"+ fullName);

 		    if(path==null)
		    {
		    //	Debug.LogWarning("materials/"+name+".tth: Not Found");
			    return null;
		    }

            BR = new BinaryReader (File.Open (path, FileMode.Open));

            BR.BaseStream.Seek (0, SeekOrigin.Begin);
            troikaTextureHeader tkheader = ReadTroikaTextureHeader (BR);

            BR.BaseStream.Seek (-tkheader.VTFSize, SeekOrigin.End);
            vtfheader header = ReadHeader (BR);

            BR.BaseStream.Dispose();

            if (!name.Contains(".ttz"))
                fullName = name + ".ttz";

            path = ResourceManager.GetPath("materials/" + fullName);

            if (path == null)
            {
                //	Debug.LogWarning("materials/"+name+".tth: Not Found");
                return null;
            }

            MemoryStream outputMemoryStream = new MemoryStream();

            byte[] queueBuffer = new byte[4096]; 
            DeflateStream zstream = new DeflateStream(File.Open(path, FileMode.Open), CompressionMode.Decompress);
            while (true)
            {
                int readBytes = zstream.Read(queueBuffer, 0, 4096);
                if (readBytes == 0)
                {
                    break;
                }
                else
                {
                    outputMemoryStream.Write(queueBuffer, 0, readBytes);
                }
            }
            zstream.Dispose();

            BR = new BinaryReader (outputMemoryStream);

            BR.BaseStream.Seek (0, SeekOrigin.Begin);

		    byte[] ImageData;
		    uint ImageDataSize;
		    byte[] ThumbnailImageData;
		    uint ThumbnailDataSize;

		    ReadData (BR, header,out ImageData, out ImageDataSize, out ThumbnailImageData, out ThumbnailDataSize);

		    BR.BaseStream.Dispose ();
		    //return CreateThumbnailTexture (name);

		    return CreateTexture (fullName,header,ImageData);
        }
	}

 	static troikaTextureHeader ReadTroikaTextureHeader(BinaryReader BR)
    {
        troikaTextureHeader header = new troikaTextureHeader ();
        
        header.signature = new string (BR.ReadChars(4));
        header.numVTF = BR.ReadUInt16();
        header.flags = BR.ReadUInt16();
        header.VTFSize = BR.ReadUInt32();

        return header;
    }

	static vtfheader ReadHeader(BinaryReader BR)
	{
		//BR.BaseStream.Seek (0, SeekOrigin.Begin);
		vtfheader header = new vtfheader ();

		header.signature = new string (BR.ReadChars(4));
		header.version = new uint[]{BR.ReadUInt32(), BR.ReadUInt32()};
		header.headerSize = BR.ReadUInt32 ();
		header.width = (short)BR.ReadUInt16 ();
		header.height = (short)BR.ReadUInt16 ();
		header.flags = BR.ReadUInt32 ();
		header.frames = BR.ReadUInt16 ();
		header.firstFrame = BR.ReadUInt16 ();
		header.padding0 = BR.ReadBytes(4);
		header.reflectivity = new Vector3 (BR.ReadSingle(), BR.ReadSingle(), BR.ReadSingle());
		header.padding1 = BR.ReadBytes (4);
		header.bumpmapScale = BR.ReadSingle ();
		header.highResImageFormat = (VTFImageFormat)BR.ReadUInt32 ();
		header.mipmapCount = BR.ReadByte ();
		header.lowResImageFormat = (VTFImageFormat)BR.ReadUInt32 ();
		header.lowResImageWidth = BR.ReadByte ();
		header.lowResImageHeight = BR.ReadByte ();
		header.depth = BR.ReadUInt16 ();

		return header;
	}

	static void ReadData(BinaryReader BR, vtfheader header, out byte[] data, out uint dataSize, out byte[] ThumbData, out uint thumbSize)
	{
		dataSize = ComputeImageSize (header.width, header.height, header.mipmapCount, header.frames, header.highResImageFormat);

		thumbSize = ComputeImageSize (header.lowResImageWidth, header.lowResImageHeight, header.lowResImageFormat);

		uint ThumbnailDataOffset = 0;
		uint ImageDataOffset = 0;

		ThumbnailDataOffset = header.headerSize;
		ImageDataOffset = ThumbnailDataOffset + thumbSize;

		BR.BaseStream.Seek (ThumbnailDataOffset, SeekOrigin.Begin);
		ThumbData = BR.ReadBytes ((int)thumbSize);


		BR.BaseStream.Seek (ImageDataOffset, SeekOrigin.Begin);
		data = BR.ReadBytes ((int)dataSize);
	}

    static int CalculateVTFMipOffset(short width, short height, byte mipCount) 
    {
        int mipOffset = 0;
        for(int i = mipCount-1; i>0;i--)
        {
            mipOffset += (width / (1 << i)) * (height / (1 << i));
        }
        return mipOffset;
    }

	static Texture2D CreateTexture(string name, vtfheader header, byte[] ImageData)
	{
        bool isLinear = false;
        bool usesMipMaps = true;

		Texture2D temp;

        if ((header.flags & IMAGE_FLAG_NORMAL_MAP) != 0)
        {
            isLinear = true;
        }

		if (header.highResImageFormat == VTFImageFormat.IMAGE_FORMAT_DXT5) 
		{
			temp=new Texture2D((int)header.width, (int)header.height, TextureFormat.DXT5,false);
			int offset = (header.width*header.height);
			byte[] buf = new byte[offset];
			Buffer.BlockCopy (ImageData,ImageData.Length-offset,buf,0,offset);
			temp.LoadRawTextureData(buf);
			temp.Apply(true);
			//if(remipmap)
			//{
				Color32[] tempCol = temp.GetPixels32();
				temp = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA32, usesMipMaps, isLinear);
				temp.SetPixels32(tempCol);
				temp.Apply(true);
				temp.Compress(true);
			//}
			temp.name = name;

			//if ((header.flags & 4) != 0)
			//	temp.
			//Debug.Log("Texture loaded "+header.width+"x"+header.height);
			return temp;
		}
		if (header.highResImageFormat == VTFImageFormat.IMAGE_FORMAT_DXT1) 
		{
			temp = new Texture2D((int)header.width, (int)header.height, TextureFormat.DXT1,false);
			int offset = ((header.width*header.height)/2)*header.frames;
			byte[] buf = new byte[offset];
			Buffer.BlockCopy (ImageData,ImageData.Length-offset,buf,0,offset);
			temp.LoadRawTextureData(buf);
			temp.Apply();
			//if(remipmap)
			//{
				Color32[] tempCol = temp.GetPixels32();
				temp = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGBA32, usesMipMaps, isLinear);
				temp.SetPixels32(tempCol);
				temp.Apply(true);
				temp.Compress(true);
			//}
			temp.name = name;
			//Debug.Log("Texture loaded "+header.width+"x"+header.height);
			return temp;
		}
		if (header.highResImageFormat == VTFImageFormat.IMAGE_FORMAT_BGR888) 
		{
            int mipOffset = CalculateVTFMipOffset(header.width, header.height, header.mipmapCount);

			temp = new Texture2D((int)header.width, (int)header.height, TextureFormat.RGB24,usesMipMaps,isLinear);
			Color32[] colors = new Color32[header.width * header.height];
			for(int i=0; i<header.width * header.height; i++)
			{
				colors[i] = new Color32(ImageData[(mipOffset*3)+(i*3)+2],ImageData[(mipOffset*3)+(i*3)+1],ImageData[(mipOffset*3)+i*3],255);
			}
			temp.SetPixels32(colors);
			temp.Apply ();
			temp.name = name;
			//Debug.Log("Texture loaded "+header.width+"x"+header.height);
			return temp;
		}
		if (header.highResImageFormat == VTFImageFormat.IMAGE_FORMAT_BGRA8888) 
		{
			temp = new Texture2D((int)header.width, (int)header.height, TextureFormat.BGRA32,false, isLinear);
			/*Color32[] colors = new Color32[header.width * header.height];
			for(int i=0; i<header.width * header.height; i++)
			{
				colors[i] = new Color32(ImageData[i*3+2],ImageData[i*3+1],ImageData[i*3],ImageData[i*3+3]);
			}
			temp.SetPixels32(colors);*/
			int imageSize=header.width*header.height*4;
			//int offset = imageSize*header.frames;
			Debug.Log("ImageData length "+ImageData.Length/*+" offset "+offset*/+" Image size "+imageSize+" frames "+header.frames);
			byte[] buf = new byte[imageSize];
			Buffer.BlockCopy (ImageData,ImageData.Length-imageSize,buf,0,imageSize);
			temp.LoadRawTextureData(buf);
			temp.Apply ();
			temp.name = name;
			//Debug.Log("Texture loaded "+header.width+"x"+header.height);
			return temp;
		}
		Debug.LogWarning (name+" Unsuported Texture Format" + header.highResImageFormat);
		return null;
	}

	/*Texture2D CreateThumbnailTexture(string name, vtfheader header)
	{
		Texture2D temp;
		if (header.lowResImageFormat == VTFImageFormat.IMAGE_FORMAT_DXT5) 
		{
			temp=new Texture2D((int)header.lowResImageWidth, (int)header.lowResImageHeight, TextureFormat.DXT5,false);
			temp.LoadRawTextureData(ThumbnailImageData);
			temp.Apply();
			temp.filterMode=FilterMode.Point;
			temp.name=name;
			//Debug.Log("Texture loaded "+header.lowResImageWidth+"x"+header.lowResImageHeight);
			return temp;
		}
		if (header.lowResImageFormat == VTFImageFormat.IMAGE_FORMAT_DXT1) 
		{
			temp=new Texture2D((int)header.lowResImageWidth, (int)header.lowResImageHeight, TextureFormat.DXT1, false);
			temp.LoadRawTextureData(ThumbnailImageData);
			temp.Apply();
			temp.filterMode=FilterMode.Point;
			temp.name=name;
			//Debug.Log("Texture loaded "+header.lowResImageWidth+"x"+header.lowResImageHeight);
			return temp;
		}
		return null;
	}*/


	static uint ComputeImageSize(int Width, int Height, VTFImageFormat imageFormat)
	{
		switch(imageFormat)
		{
		case VTFImageFormat.IMAGE_FORMAT_DXT1:
		case VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA:
			if(Width < 4 && Width > 0)
				Width = 4;
			
			if(Height < 4 && Height > 0)
				Height = 4;

			return (((uint)Width + 3) / 4) * (((uint)Height + 3) / 4) * 8;
			//return (uint)Width * (uint)Height * 1;
		case VTFImageFormat.IMAGE_FORMAT_DXT3:
		case VTFImageFormat.IMAGE_FORMAT_DXT5:
			if(Width < 4 && Width > 0)
				Width = 4;
			
			if(Height < 4 && Height > 0)
				Height = 4;

			return (((uint)Width + 3) / 4) * (((uint)Height + 3) / 4) * 16;
		case VTFImageFormat.IMAGE_FORMAT_ABGR8888:
		case VTFImageFormat.IMAGE_FORMAT_ARGB8888:
		case VTFImageFormat.IMAGE_FORMAT_RGBA8888:
		case VTFImageFormat.IMAGE_FORMAT_BGRA8888:
		case VTFImageFormat.IMAGE_FORMAT_BGRX8888:
		case VTFImageFormat.IMAGE_FORMAT_UVWQ8888:
		case VTFImageFormat.IMAGE_FORMAT_UVLX8888:
		case VTFImageFormat.IMAGE_FORMAT_R32F:
		case VTFImageFormat.IMAGE_FORMAT_NV_INTZ:
		case VTFImageFormat.IMAGE_FORMAT_NV_RAWZ:
		case VTFImageFormat.IMAGE_FORMAT_NV_NULL:
			return (uint)Width*(uint)Height * 4;
		case VTFImageFormat.IMAGE_FORMAT_RGB888:
		case VTFImageFormat.IMAGE_FORMAT_BGR888:
		case VTFImageFormat.IMAGE_FORMAT_RGB888_BLUESCREEN:
		case VTFImageFormat.IMAGE_FORMAT_BGR888_BLUESCREEN:
		case VTFImageFormat.IMAGE_FORMAT_NV_DST24:
			return (uint)Width*(uint)Height*3;
		case VTFImageFormat.IMAGE_FORMAT_RGB565:
		case VTFImageFormat.IMAGE_FORMAT_IA88:
		case VTFImageFormat.IMAGE_FORMAT_BGR565:
		case VTFImageFormat.IMAGE_FORMAT_BGRX5551:
		case VTFImageFormat.IMAGE_FORMAT_BGRA4444:
		case VTFImageFormat.IMAGE_FORMAT_BGRA5551:
		case VTFImageFormat.IMAGE_FORMAT_UV88:
		case VTFImageFormat.IMAGE_FORMAT_NV_DST16:
		case VTFImageFormat.IMAGE_FORMAT_ATI_DST16:
			return(uint) Width*(uint)Height*2;
		default:
			return(uint) Width * (uint)Height * 1;//GetImageFormatInfo(imageFormat).BytesPerPixel;
		}
	}

	static uint ComputeImageSize(int width, int height, int mipmaps, uint frames, VTFImageFormat imageFormat)
	{
		uint imageSize = 0;

		for(uint i=0;i<mipmaps;i++)
		{
			imageSize+=ComputeImageSize(width,height,imageFormat);

			width>>=1;
			height>>=1;

			if(width < 1)
				width = 1;
			
			if(height < 1)
				height = 1;
		}
		imageSize *= frames;
		return imageSize;
	}
}

}