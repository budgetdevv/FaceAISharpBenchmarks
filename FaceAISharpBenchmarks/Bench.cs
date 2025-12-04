using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FaceAiSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#if OS_IS_WINDOWS
using BenchmarkDotNet.Diagnostics.Windows.Configs;
#endif

namespace FaceAISharpBenchmarks
{
    [MemoryDiagnoser]
    #if OS_IS_WINDOWS
    [NativeMemoryProfiler]
    [InliningDiagnoser(logFailuresOnly: true, filterByNamespace: true)]
    [DisassemblyDiagnoser(exportDiff: true, exportCombinedDisassemblyReport: true)]
    #endif
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class Bench
    {
        [ModuleInitializer]
        public static void RunCctor()
        {
            RuntimeHelpers.RunClassConstructor(typeof(Bench).TypeHandle);
        }

        private static readonly string EXECUTION_DIRECTORY = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        private static readonly ArcFaceEmbeddingsGenerator
            FULL_FACE_EMBEDDING_MODEL,
            QUANTIZED_FACE_EMBEDDING_MODEL;

        private static readonly Image<Rgb24>[] ALIGNED_FACES;

        static Bench()
        {
            var fullModelPath = ConstructModelPath("arcfaceresnet100-8.onnx");

            var quantizedModelPath = ConstructModelPath("arcfaceresnet100-11-int8.onnx");

            var faceDetector = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();

            FULL_FACE_EMBEDDING_MODEL = new ArcFaceEmbeddingsGenerator(
                options: new() { ModelPath = fullModelPath }
            );

            QUANTIZED_FACE_EMBEDDING_MODEL = new ArcFaceEmbeddingsGenerator(
                options: new() { ModelPath = quantizedModelPath }
            );

            var imagePath = Path.Combine(EXECUTION_DIRECTORY, "Assets", "TPIIT.jpeg");

            var image = Image.Load<Rgb24>(path: imagePath);

            var detectedFaces = faceDetector.DetectFaces(image);

            var alignedFacesList = new List<Image<Rgb24>>();

            foreach (var detectedFace in detectedFaces)
            {
                // Copy since AlignFaceUsingLandmarks() is mutating...
                var alignedFace = image.Clone();

                ArcFaceEmbeddingsGenerator.AlignFaceUsingLandmarks(
                    face: alignedFace,
                    landmarks: detectedFace.Landmarks
                );

                alignedFacesList.Add(alignedFace);
            }

            ALIGNED_FACES = alignedFacesList.ToArray();

            return;

            static string ConstructModelPath(string modelFileName)
            {
                return Path.Combine(EXECUTION_DIRECTORY, "onnx", modelFileName);
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            GC.Collect();

            GC.WaitForPendingFinalizers();

            GC.Collect();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FakeConsume(float[] embedding)
        {
            // Prevent the compiler from optimizing away the embedding generation.
        }

        [Benchmark]
        public void FullFaceEmbeddingModel()
        {
            foreach (var alignedFace in ALIGNED_FACES)
            {
                FakeConsume(FULL_FACE_EMBEDDING_MODEL.GenerateEmbedding(alignedFace));
            }
        }

        [Benchmark]
        public void INT8QuantizedFaceEmbeddingModel()
        {
            foreach (var alignedFace in ALIGNED_FACES)
            {
                FakeConsume(QUANTIZED_FACE_EMBEDDING_MODEL.GenerateEmbedding(alignedFace));
            }
        }
    }
}