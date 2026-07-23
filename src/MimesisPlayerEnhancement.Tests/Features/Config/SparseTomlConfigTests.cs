using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class SparseTomlConfigTests
    {
        [Fact]
        public void Load_parses_sections_keys_comments_and_quoted_strings()
        {
            const string text = """
                # comment
                [MimesisPlayerEnhancement_Economy]
                EnableEconomy = true
                StartupMoneyMultiplier = "2.5"
                """;

            SparseTomlConfig.Document doc = SparseTomlConfig.Load(text);

            Assert.Single(doc.SectionOrder);
            Assert.Equal("MimesisPlayerEnhancement_Economy", doc.SectionOrder[0]);
            Assert.True(doc.Sections.TryGetValue("MimesisPlayerEnhancement_Economy", out Dictionary<string, string>? keys));
            Assert.NotNull(keys);
            Assert.Equal("true", keys["EnableEconomy"]);
            Assert.Equal("2.5", keys["StartupMoneyMultiplier"]);
        }

        [Fact]
        public void SerializeRaw_round_trips_document_order()
        {
            SparseTomlConfig.Document doc = new();
            doc.SectionOrder.Add("SectionA");
            doc.Sections["SectionA"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["KeyB"] = "b",
                ["KeyA"] = "a",
            };

            string text = SparseTomlConfig.SerializeRaw(doc);
            SparseTomlConfig.Document loaded = SparseTomlConfig.Load(text);

            Assert.Equal(["SectionA"], loaded.SectionOrder);
            Assert.Equal("a", loaded.Sections["SectionA"]["KeyA"]);
            Assert.Equal("b", loaded.Sections["SectionA"]["KeyB"]);
        }

        [Fact]
        public void IsEmpty_returns_true_for_blank_document()
        {
            Assert.True(SparseTomlConfig.IsEmpty(new SparseTomlConfig.Document()));
            Assert.True(SparseTomlConfig.IsEmpty(SparseTomlConfig.Load(null)));
        }

        [Fact]
        public void IsEmpty_returns_false_when_gameplay_section_has_keys()
        {
            SparseTomlConfig.Document doc = SparseTomlConfig.Load("""
                [MimesisPlayerEnhancement_Economy]
                EnableEconomy = true
                """);

            Assert.False(SparseTomlConfig.IsEmpty(doc));
        }

        [Fact]
        public void TryRepairAssignmentLine_quotes_bare_strings()
        {
            Assert.True(SparseTomlConfig.TryRepairAssignmentLine("Mode = custom", out string repaired));
            Assert.Equal("Mode = \"custom\"", repaired);
        }

        [Theory]
        [InlineData("EnableEconomy = true")]
        [InlineData("MaxPlayers = 8")]
        public void TryRepairAssignmentLine_returns_false_for_bools_and_numbers(string line)
        {
            Assert.False(SparseTomlConfig.TryRepairAssignmentLine(line, out _));
        }

        [Fact]
        public void TryRepairAssignmentLine_returns_false_for_section_headers()
        {
            Assert.False(SparseTomlConfig.TryRepairAssignmentLine("[Section]", out _));
        }
    }
}
