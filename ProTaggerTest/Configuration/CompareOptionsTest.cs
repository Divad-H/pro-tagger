using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProTagger.Configuration;
using ProTaggerTest.Mocks;
using System;

namespace ProTaggerTest.Configuration
{
    [TestClass]
    public class CompareOptionsTest
    {
        [TestMethod]
        public void ChangeSettings()
        {
            var schedulers = new TestSchedulers();
            CompareOptions? result = null;
            var options = new CompareOptionsViewModel(schedulers, new CompareOptions()
            {
                Algorithm = DiffAlgorithm.Minimal,
                ContextLines = 3,
                IncludeUnmodified = false,
                IndentHeuristic = true,
                InterhunkLines = 1,
                Similarity = new SimilarityOptions()
                {
                    BreakRewriteThreshold = 1,
                    CopyThreshold = 2,
                    RenameDetectionMode = RenameDetectionMode.Copies,
                    RenameFromRewriteThreshold = 3,
                    RenameLimit = 4,
                    RenameThreshold = 5,
                    WhitespaceMode = WhitespaceMode.DontIgnoreWhitespace,
                }
            });
            using var _ = options.CompareOptionsObservable.Subscribe(options => result = options);
            Assert.IsNotNull(result);
            if (result == null)
                throw new Exception();
            Assert.AreEqual(3, result.ContextLines);
            options.DiffAlgorithm.Value = DiffAlgorithm.Minimal;
            Assert.AreEqual(DiffAlgorithm.Minimal, result.Algorithm);
            options.ContextLinesInput.Text = "10";
            Assert.AreEqual(10, result.ContextLines);
            options.IndentHeuristic = false;
            Assert.IsFalse(result.IndentHeuristic);
            options.InterhunkLinesInput.Text = "11";
            Assert.AreEqual(11, result.InterhunkLines);
            options.SimilarityOptions.BreakRewriteThresholdInput.Text = "12";
            Assert.AreEqual(12, result.Similarity.BreakRewriteThreshold);
            options.SimilarityOptions.CopyThresholdInput.Text = "13";
            Assert.AreEqual(13, result.Similarity.CopyThreshold);
            options.SimilarityOptions.RenameDetectionMode.Value = RenameDetectionMode.Exact;
            Assert.AreEqual(RenameDetectionMode.Exact, result.Similarity.RenameDetectionMode);
            options.SimilarityOptions.RenameFromRewriteThresholdInput.Text = "14";
            Assert.AreEqual(14, result.Similarity.RenameFromRewriteThreshold);
            options.SimilarityOptions.RenameLimitInput.Text = "15";
            Assert.AreEqual(15, result.Similarity.RenameLimit);
            options.SimilarityOptions.RenameThresholdInput.Text = "16";
            Assert.AreEqual(16, result.Similarity.RenameThreshold);
            options.SimilarityOptions.WhitespaceMode.Value = WhitespaceMode.IgnoreAllWhitespace;
            Assert.AreEqual(WhitespaceMode.IgnoreAllWhitespace, result.Similarity.WhitespaceMode);
        }
    }
}
