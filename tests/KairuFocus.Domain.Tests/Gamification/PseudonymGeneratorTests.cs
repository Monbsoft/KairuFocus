using System.Globalization;
using KairuFocus.Domain.Gamification;

namespace KairuFocus.Domain.Tests.Gamification;

public sealed class PseudonymGeneratorTests
{
    [Fact]
    public void Should_ProduceValidPseudonym_When_AnyAnimalIsCombinedWithAnyQuality()
    {
        for (var a = 0; a < PseudonymGenerator.Animals.Count; a++)
        {
            for (var q = 0; q < PseudonymGenerator.Qualities.Count; q++)
            {
                var plain = PseudonymGenerator.Compose(a, q).Value;
                var longestSuffixed = PseudonymGenerator.Compose(a, q, PseudonymGenerator.MaxSuffix).Value;

                Assert.True(Pseudonym.Create(plain).IsSuccess, plain);
                Assert.True(Pseudonym.Create(longestSuffixed).IsSuccess, longestSuffixed);
            }
        }
    }

    [Theory]
    [InlineData("Loutre", "Attentif", "LoutreAttentive")]
    [InlineData("Mesange", "Vif", "MesangeVive")]
    [InlineData("Tortue", "Serein", "TortueSereine")]
    [InlineData("Abeille", "Studieux", "AbeilleStudieuse")]
    [InlineData("Loutre", "Tenace", "LoutreTenace")]
    public void Should_UseFeminineForm_When_AnimalIsFeminine(string animal, string masculineQuality, string expected)
    {
        var animalIndex = IndexOfAnimal(animal);
        Assert.True(PseudonymGenerator.Animals[animalIndex].IsFeminine);

        var pseudonym = PseudonymGenerator.Compose(animalIndex, IndexOfQuality(masculineQuality));

        Assert.Equal(expected, pseudonym.Value);
    }

    [Theory]
    [InlineData("Hibou", "Attentif", "HibouAttentif")]
    [InlineData("Lynx", "Vif", "LynxVif")]
    [InlineData("Heron", "Patient", "HeronPatient")]
    [InlineData("Castor", "Studieux", "CastorStudieux")]
    [InlineData("Hibou", "Tenace", "HibouTenace")]
    public void Should_UseMasculineForm_When_AnimalIsMasculine(string animal, string masculineQuality, string expected)
    {
        var animalIndex = IndexOfAnimal(animal);
        Assert.False(PseudonymGenerator.Animals[animalIndex].IsFeminine);

        var pseudonym = PseudonymGenerator.Compose(animalIndex, IndexOfQuality(masculineQuality));

        Assert.Equal(expected, pseudonym.Value);
    }

    [Fact]
    public void Should_OfferAtLeast1200DistinctCombinations_When_ListsAreCombined()
    {
        var combinations = new List<string>();
        for (var a = 0; a < PseudonymGenerator.Animals.Count; a++)
        {
            for (var q = 0; q < PseudonymGenerator.Qualities.Count; q++)
                combinations.Add(PseudonymGenerator.Compose(a, q).Value);
        }

        // Case-insensitive: the unique index relies on a case-insensitive database collation.
        var distinct = combinations.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        Assert.True(PseudonymGenerator.Animals.Count >= 40, $"{PseudonymGenerator.Animals.Count} animals");
        Assert.True(PseudonymGenerator.Qualities.Count >= 30, $"{PseudonymGenerator.Qualities.Count} qualities");
        Assert.True(distinct >= 1200, $"{distinct} distinct combinations");
        Assert.Equal(combinations.Count, distinct);
    }

    [Fact]
    public void Should_ProduceSameSequence_When_RandomSeedIsSame()
    {
        var first = new Random(42);
        var second = new Random(42);

        var firstSequence = Enumerable.Range(0, 50).Select(_ => PseudonymGenerator.Generate(first)).ToList();
        var secondSequence = Enumerable.Range(0, 50).Select(_ => PseudonymGenerator.Generate(second)).ToList();

        Assert.Equal(firstSequence, secondSequence);
    }

    [Theory]
    [InlineData(PseudonymGenerator.MinSuffix)]
    [InlineData(27)]
    [InlineData(PseudonymGenerator.MaxSuffix)]
    public void Should_AppendSuffix_When_GeneratingWithSuffix(int suffix)
    {
        var plain = PseudonymGenerator.Generate(new Random(7));

        var suffixed = PseudonymGenerator.GenerateWithSuffix(new Random(7), suffix);

        Assert.Equal(plain.Value + suffix.ToString(CultureInfo.InvariantCulture), suffixed.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void Should_Throw_When_SuffixIsOutOfRange(int suffix)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PseudonymGenerator.GenerateWithSuffix(new Random(7), suffix));
    }

    private static int IndexOfAnimal(string name)
    {
        for (var i = 0; i < PseudonymGenerator.Animals.Count; i++)
        {
            if (PseudonymGenerator.Animals[i].Name == name)
                return i;
        }

        throw new InvalidOperationException($"Animal '{name}' is not in the list.");
    }

    private static int IndexOfQuality(string masculine)
    {
        for (var i = 0; i < PseudonymGenerator.Qualities.Count; i++)
        {
            if (PseudonymGenerator.Qualities[i].Masculine == masculine)
                return i;
        }

        throw new InvalidOperationException($"Quality '{masculine}' is not in the list.");
    }
}
