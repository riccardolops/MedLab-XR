using UnityEngine;
using UnityVolumeRendering;
using Kitware.VTK;

using UnityEditor;
using itk.simple;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using Dummiesman;

public class RegionGrowing
{
    private Mesh PolyDataToMesh(vtkPolyData pd, Mesh mesh)
    {
        // Points / Vertices
        long numVertices = pd.GetNumberOfPoints();
        Debug.Log(numVertices);
        Vector3[] vertices = new Vector3[numVertices];
        for (long i = 0; i < numVertices; ++i)
        {
            double[] pnt = pd.GetPoint(i);
			// Flip z-up to y-up
			vertices[i] = new Vector3(-(float)(pnt[0] * 1000), (float)(pnt[2] * 1000), (float)(pnt[1] * 1000));
        }
        mesh.vertices = vertices;

        
/*         Vector3[] vertices = new Vector3[numVtx];
        vtkCellArray verticesCellArray = pd.GetVerts();
        if (verticesCellArray.GetNumberOfCells() > 0)
		{
            verticesCellArray.InitTraversal();
            while (verticesCellArray.GetNextCell(verticesCellArray) != 0)
        } */


        // Texture coordinates
		vtkDataArray vtkTexCoords = pd.GetPointData().GetTCoords();
		if (vtkTexCoords != null)
		{
			long numCoords = vtkTexCoords.GetNumberOfTuples();
			Vector2[] uvs = new Vector2[numCoords];
			for (int i = 0; i < numCoords; ++i)
			{
				double[] texCoords = vtkTexCoords.GetTuple2(i);
				uvs[i] = new Vector2((float)texCoords[0], (float)texCoords[1]);
			}
			mesh.uv = uvs;
		}

		// Triangles / Cells

        long numTriangles = pd.GetNumberOfPolys();
        Debug.Log(numTriangles);
		vtkCellArray polys = pd.GetPolys();
		if (polys.GetNumberOfCells() > 0)
		{
			int[] triangles = new int[numTriangles * 3];
			int prim = 0;
			vtkIdList pts = vtkIdList.New();
			polys.InitTraversal();
			while (polys.GetNextCell(pts) != 0)
			{
                if (pts.GetNumberOfIds() == 3)
                {
                    for (int i = 0; i < pts.GetNumberOfIds(); ++i)
					triangles[prim * 3 + i] = (int)pts.GetId(i);

				    ++prim;
                }
                else
                {
                    Debug.LogError("Not a triangle");
                }
			}
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}
		
		// Lines
		vtkCellArray lines = pd.GetLines();
		if (lines.GetNumberOfCells() > 0)
		{
			ArrayList idList = new ArrayList();
			vtkIdList pts = vtkIdList.New();
			lines.InitTraversal();
			while (lines.GetNextCell(pts) != 0)
			{
				for (int i = 0; i < pts.GetNumberOfIds() - 1; ++i)
				{
					idList.Add(pts.GetId(i));
					idList.Add(pts.GetId(i+1));
				}
			}

			mesh.SetIndices(idList.ToArray(typeof(int)) as int[], MeshTopology.Lines, 0);
			mesh.RecalculateBounds();
			return mesh;
		}

		// Points
		vtkCellArray points = pd.GetVerts();
		long numPointCells = points.GetNumberOfCells();
		if (numPointCells > 0)
		{
			ArrayList idList = new ArrayList();
			vtkIdList pts = vtkIdList.New();
			points.InitTraversal();
			while (points.GetNextCell(pts) != 0)
			{
				for (int i = 0; i < pts.GetNumberOfIds(); ++i)
				{
					idList.Add(pts.GetId(i));
				}
			}

			mesh.SetIndices(idList.ToArray(typeof(int)) as int[], MeshTopology.Points, 0);
			mesh.RecalculateBounds();
			return mesh;
		}
        return mesh;
    }
    public async Task<string> regiongrowing(string filePath, double windowMinimum, double windowMaximum, uint[] seed)
    {
        vtkPolyData vtkPoly = new vtkPolyData();
        string oldDirectory = Directory.GetCurrentDirectory();
        await Task.Run(() =>
            {
                ImageFileReader reader = new ImageFileReader();
                reader.SetFileName(filePath);
                Image image = reader.Execute();
                SimpleITK.DICOMOrient(image, "LPS");
                Image windowedImage = SimpleITK.IntensityWindowing(image,windowMinimum, windowMaximum,0,255);
                VectorUInt32 uintseed = new VectorUInt32(seed);

                ConfidenceConnectedImageFilter rgfilter = new ConfidenceConnectedImageFilter();
                rgfilter.SetInitialNeighborhoodRadius(2);
                rgfilter.SetMultiplier(1);
                rgfilter.SetNumberOfIterations(1);
                rgfilter.SetReplaceValue(1);
                rgfilter.AddSeed(uintseed);
                Image mask = rgfilter.Execute(windowedImage);

                KernelEnum kernelType = KernelEnum.sitkBall;
                BinaryDilateImageFilter binaryDilateImageFilter = new BinaryDilateImageFilter();
                binaryDilateImageFilter.SetKernelType(kernelType);
                binaryDilateImageFilter.SetKernelRadius(1);
                Image maskb = binaryDilateImageFilter.Execute(mask);
                
                maskb = SimpleITK.Cast(maskb, PixelIDValueEnum.sitkVectorUInt8);
                VectorDouble spacing= mask.GetSpacing();

                VectorUInt32 size = mask.GetSize();
                int len = 1;
                for (int dim = 0; dim < mask.GetDimension(); dim++)
                {
                    len *= (int)size[dim];
                }
                IntPtr ptr = mask.GetBufferAsUInt8();
                /* byte[] bufferAsArray = new byte[len];
                Marshal.Copy(ptr, bufferAsArray, 0, len);
                ///
                int max = bufferAsArray.Max();
                Debug.Log(max);
                int min = bufferAsArray.Min();
                Debug.Log(min);
                /// */
                vtkImageImport dataImporter = new vtkImageImport();
                dataImporter.CopyImportVoidPointer(ptr, len);
                dataImporter.SetDataScalarTypeToUnsignedChar();
                dataImporter.SetDataExtent(0, (int)size[0]-1, 0, (int)size[1]-1, 0, (int)size[2]-1);
                dataImporter.SetWholeExtent(0, (int)size[0]-1, 0, (int)size[1]-1, 0, (int)size[2]-1);
                dataImporter.SetDataSpacing(spacing[0], spacing[1], spacing[2]);


                vtkMarchingCubes mcubes = new vtkMarchingCubes();
                mcubes.SetInputConnection(dataImporter.GetOutputPort());
                mcubes.SetValue(0, 1);
                mcubes.Update();

                vtkPoly = mcubes.GetOutput();
                /* vtkSTLWriter writer = new vtkSTLWriter();
                writer.SetFileName("test1.stl");
                writer.SetInputConnection(mcubes.GetOutputPort());
                writer.Update();
                writer.Write(); */
                vtkOBJExporter vtkSave = new vtkOBJExporter();
                vtkRenderWindow renWin = vtkRenderWindow.New();
                vtkRenderer ren = vtkRenderer.New();
                renWin.AddRenderer(ren);
                vtkActor actor = vtkActor.New();
                vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
                mapper.SetInputConnection(mcubes.GetOutputPort());
                actor.SetMapper(mapper);
                ren.AddActor(actor);
                vtkSave.SetInput(renWin);
                vtkSave.SetFilePrefix("mesh");
                oldDirectory = Directory.GetCurrentDirectory();
                vtkSave.Update();
                vtkSave.Write();
            });
        return oldDirectory;
        /* Mesh newMesh = new Mesh();
        newMesh = PolyDataToMesh(vtkPoly,newMesh);
        return newMesh; */
        /* vtkPolyDataWriter vtkSave = new vtkPolyDataWriter();
        vtkSave.SetInputConnection(mcubes.GetOutputPort());
        vtkSave.SetFileName("testtttt.vtk");
        vtkSave.Update();
        vtkSave.Write(); */

        /* vtkSTLWriter writer = new vtkSTLWriter();
        writer.SetFileName("testtttt.stl");
        writer.SetInputConnection(mcubes.GetOutputPort());
        writer.Update();
        writer.Write(); */
    }
}
