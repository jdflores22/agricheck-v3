namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// Full Philippine MAV / SPS HS library (AHTN 2022 6-digit level).
/// Sources: WTO ILC PH11 (14 MAV products), DA AC 06 s2022 agency jurisdiction, AHTN 2022 structure.
/// </summary>
internal static class MavHsLibraryCatalog
{
    private const string OfficialMav =
        "Official DA Minimum Access Volume (MAV) product under WTO Agreement on Agriculture.";

    private const string BfarSps =
        "BFAR-regulated fishery/aquatic product (SPS Import Clearance). Full Chapter 03 coverage.";

    private const string BpiSps =
        "BPI-regulated plant product (SPS Import Clearance). Extended plant import coverage.";

    private const string SraMav =
        "Official DA MAV product administered by Sugar Regulatory Administration (SRA).";

    private const string NtaTobacco =
        "NTA-regulated tobacco product for import/export licensing and industry regulation.";

    public static IReadOnlyList<MavHsLibrarySeedCategory> Build() =>
        new List<MavHsLibrarySeedCategory>()
            .Concat(BaiOfficialMav())
            .Concat(BpiOfficialMav())
            .Concat(BpiExtendedPlants())
            .Concat(BfarChapter03())
            .Concat(SraOfficialMav())
            .Concat(NtaTobaccoProducts())
            .ToList();

    private static IEnumerable<MavHsLibrarySeedCategory> BaiOfficialMav()
    {
        yield return Cat("0101", "Live horses, asses, mules and hinnies", "BAI", OfficialMav,
            H("21", "Pure-bred breeding horses"),
            H("29", "Other horses"),
            H("30", "Asses"),
            H("90", "Other live horses, asses, mules and hinnies"));

        yield return Cat("0102", "Live bovine animals", "BAI", OfficialMav,
            H("21", "Pure-bred breeding cattle"),
            H("29", "Other cattle"),
            H("31", "Pure-bred breeding buffalo"),
            H("39", "Other buffalo"),
            H("90", "Other live bovine animals"));

        yield return Cat("0103", "Live swine", "BAI", OfficialMav,
            H("10", "Pure-bred breeding swine"),
            H("91", "Other live swine weighing less than 50 kg"),
            H("92", "Other live swine weighing 50 kg or more"));

        yield return Cat("0104", "Live sheep and goats", "BAI", OfficialMav,
            H("10", "Live sheep"),
            H("20", "Live goats"));

        yield return Cat("0105", "Live poultry", "BAI", OfficialMav,
            H("11", "Live fowls (Gallus domesticus) weighing not more than 185 g"),
            H("12", "Live turkeys weighing not more than 185 g"),
            H("13", "Live ducks weighing not more than 185 g"),
            H("14", "Live geese weighing not more than 185 g"),
            H("15", "Live guinea fowls weighing not more than 185 g"),
            H("94", "Live fowls (Gallus domesticus) weighing more than 185 g"),
            H("95", "Live ducks, geese, turkeys and guinea fowls weighing more than 185 g"),
            H("99", "Other live poultry"));

        yield return Cat("0201", "Meat of bovine animals, fresh or chilled", "BAI", OfficialMav,
            H("10", "Carcasses and half-carcasses, fresh or chilled"),
            H("20", "Other cuts with bone in, fresh or chilled"),
            H("30", "Boneless cuts, fresh or chilled"));

        yield return Cat("0202", "Meat of bovine animals, frozen", "BAI", OfficialMav,
            H("10", "Carcasses and half-carcasses, frozen"),
            H("20", "Other cuts with bone in, frozen"),
            H("30", "Boneless cuts, frozen"));

        yield return Cat("0203", "Meat of swine, fresh, chilled or frozen", "BAI", OfficialMav,
            H("11", "Fresh or chilled hams, shoulders and cuts thereof, with bone in"),
            H("12", "Fresh or chilled hams, shoulders and cuts thereof, boneless"),
            H("19", "Other fresh or chilled meat of swine"),
            H("21", "Frozen carcasses and half-carcasses of swine"),
            H("22", "Frozen hams, shoulders and cuts thereof, with bone in"),
            H("29", "Other frozen meat of swine"));

        yield return Cat("0204", "Meat of sheep or goats, fresh, chilled or frozen", "BAI", OfficialMav,
            H("10", "Fresh or chilled carcasses and half-carcasses of lamb"),
            H("20", "Other fresh or chilled cuts of sheep"),
            H("30", "Frozen carcasses and half-carcasses of lamb"),
            H("40", "Other frozen cuts of sheep"),
            H("50", "Fresh, chilled or frozen meat of goats"));

        yield return Cat("0207", "Meat and edible offal of poultry, fresh, chilled or frozen", "BAI", OfficialMav,
            H("11", "Whole chickens, not cut in pieces"),
            H("12", "Whole turkeys, not cut in pieces"),
            H("13", "Cuts and offal of chickens, fresh or chilled"),
            H("14", "Cuts and offal of chickens, frozen"),
            H("24", "Whole ducks, geese or guinea fowls, not cut in pieces"),
            H("26", "Cuts and offal of turkeys, fresh or chilled"),
            H("27", "Cuts and offal of turkeys, frozen"),
            H("41", "Cuts and offal of ducks, geese or guinea fowls, fresh or chilled"),
            H("42", "Cuts and offal of ducks, geese or guinea fowls, frozen"),
            H("99", "Other poultry meat and offal"));
    }

    private static IEnumerable<MavHsLibrarySeedCategory> BpiOfficialMav()
    {
        yield return Cat("0701", "Potatoes, fresh or chilled", "BPI", OfficialMav,
            H("10", "Seed potatoes"),
            H("90", "Other potatoes, fresh or chilled"));

        yield return Cat("0901", "Coffee, whether or not roasted or decaffeinated", "BPI", OfficialMav,
            H("11", "Coffee, not roasted, not decaffeinated"),
            H("12", "Coffee, not roasted, decaffeinated"),
            H("21", "Coffee, roasted, not decaffeinated"),
            H("22", "Coffee, roasted, decaffeinated"),
            H("90", "Coffee husks and skins"));

        yield return Cat("1005", "Maize (corn)", "BPI", OfficialMav,
            H("10", "Seed maize (corn)"),
            H("90", "Other maize (corn)"));

        yield return Cat("1006", "Rice", "BPI", OfficialMav,
            H("10", "Rice in the husk (paddy or rough)"),
            H("20", "Husked (brown) rice"),
            H("30", "Semi-milled or wholly milled rice, whether or not polished or glazed"),
            H("40", "Broken rice"),
            H("50", "Flour, meal and powder of rice"),
            H("60", "Groats, meal and pellets of rice"),
            H("70", "Bran, sharps and other residues of rice"),
            H("90", "Other rice products"));
    }

    private static IEnumerable<MavHsLibrarySeedCategory> BpiExtendedPlants()
    {
        yield return Cat("0702", "Tomatoes, fresh or chilled", "BPI", BpiSps, H("00", "Tomatoes, fresh or chilled"));
        yield return Cat("0703", "Onions, shallots, garlic, leeks and other alliaceous vegetables", "BPI", BpiSps,
            H("10", "Onions and shallots"),
            H("20", "Garlic"),
            H("90", "Leeks and other alliaceous vegetables"));
        yield return Cat("0704", "Cabbages, cauliflowers, kohlrabi, kale and similar edible brassicas", "BPI", BpiSps,
            H("10", "Cauliflowers and broccoli"),
            H("90", "Other edible brassicas"));
        yield return Cat("0705", "Lettuce and chicory", "BPI", BpiSps,
            H("11", "Cabbage lettuce (head lettuce)"),
            H("19", "Other lettuce"),
            H("21", "Witloof chicory"),
            H("29", "Other chicory"));
        yield return Cat("0706", "Carrots, turnips, salad beetroot, radishes and similar edible roots", "BPI", BpiSps,
            H("10", "Carrots and turnips"),
            H("90", "Other edible roots"));
        yield return Cat("0707", "Cucumbers and gherkins, fresh or chilled", "BPI", BpiSps, H("00", "Cucumbers and gherkins, fresh or chilled"));
        yield return Cat("0708", "Leguminous vegetables, shelled or unshelled, fresh or chilled", "BPI", BpiSps, H("00", "Leguminous vegetables, fresh or chilled"));
        yield return Cat("0709", "Other vegetables, fresh or chilled", "BPI", BpiSps, H("00", "Other vegetables, fresh or chilled"));
        yield return Cat("0710", "Vegetables (uncooked or cooked by steaming or boiling in water), frozen", "BPI", BpiSps, H("00", "Frozen vegetables"));
        yield return Cat("0801", "Coconuts, Brazil nuts and cashew nuts", "BPI", BpiSps,
            H("11", "Desiccated coconuts"),
            H("19", "Other coconuts"),
            H("21", "Brazil nuts in shell"),
            H("22", "Brazil nuts shelled"),
            H("31", "Cashew nuts in shell"),
            H("32", "Cashew nuts shelled"));
        yield return Cat("0802", "Other nuts, fresh or dried", "BPI", BpiSps,
            H("11", "Almonds in shell"),
            H("12", "Almonds shelled"),
            H("21", "Hazelnuts or filberts in shell"),
            H("22", "Hazelnuts or filberts shelled"),
            H("31", "Walnuts in shell"),
            H("32", "Walnuts shelled"),
            H("90", "Other nuts"));
        yield return Cat("0803", "Bananas, including plantains, fresh or dried", "BPI", BpiSps, H("00", "Bananas and plantains"));
        yield return Cat("0804", "Dates, figs, pineapples, avocados, guavas, mangoes and mangosteens", "BPI", BpiSps,
            H("10", "Dates"),
            H("20", "Pineapples"),
            H("30", "Avocados"),
            H("40", "Mangoes and mangosteens"),
            H("90", "Other fruits of this heading"));
        yield return Cat("0805", "Citrus fruit, fresh or dried", "BPI", BpiSps,
            H("10", "Oranges"),
            H("21", "Mandarins (including tangerines and satsumas)"),
            H("29", "Other citrus fruit"));
        yield return Cat("0806", "Grapes, fresh or dried", "BPI", BpiSps,
            H("10", "Fresh grapes"),
            H("20", "Dried grapes (raisins)"));
        yield return Cat("0807", "Melons (including watermelons) and papaws (papayas), fresh", "BPI", BpiSps,
            H("11", "Watermelons"),
            H("19", "Other melons"),
            H("20", "Papaws (papayas)"));
        yield return Cat("0808", "Apples, pears and quinces, fresh", "BPI", BpiSps,
            H("10", "Apples"),
            H("20", "Pears and quinces"));
        yield return Cat("1001", "Wheat and meslin", "BPI", BpiSps,
            H("11", "Seed wheat and meslin"),
            H("19", "Other wheat and meslin"),
            H("91", "Spelt seed"),
            H("99", "Other wheat and meslin"));
        yield return Cat("1002", "Rye", "BPI", BpiSps,
            H("10", "Seed rye"),
            H("90", "Other rye"));
        yield return Cat("1003", "Barley", "BPI", BpiSps,
            H("10", "Seed barley"),
            H("90", "Other barley"));
        yield return Cat("1004", "Oats", "BPI", BpiSps,
            H("10", "Seed oats"),
            H("90", "Other oats"));
        yield return Cat("1201", "Soya beans, whether or not broken", "BPI", BpiSps,
            H("10", "Seed soya beans"),
            H("90", "Other soya beans"));
        yield return Cat("1202", "Ground-nuts, not roasted or otherwise cooked", "BPI", BpiSps,
            H("10", "Seed ground-nuts"),
            H("90", "Other ground-nuts"));
        yield return Cat("1511", "Palm oil and its fractions, crude or refined", "BPI", BpiSps,
            H("10", "Crude palm oil"),
            H("90", "Other palm oil"));
    }

    private static IEnumerable<MavHsLibrarySeedCategory> BfarChapter03()
    {
        yield return Cat("0301", "Live fish", "BFAR", BfarSps,
            H("11", "Ornamental fish, live"),
            H("19", "Trout (Salmo trutta, Oncorhynchus mykiss), live"),
            H("91", "Eels (Anguilla spp.), live"),
            H("92", "Carp (Cyprinus spp., Carassius spp.), live"),
            H("93", "Atlantic and Pacific bluefin tunas, live"),
            H("99", "Other live fish"));

        yield return Cat("0302", "Fish, fresh or chilled", "BFAR", BfarSps,
            H("11", "Trout (Salmonidae), fresh or chilled"),
            H("13", "Pacific salmon, fresh or chilled"),
            H("14", "Atlantic salmon and Danube salmon, fresh or chilled"),
            H("31", "Tunas of the genus Thunnus, fresh or chilled"),
            H("32", "Skipjack or stripe-bellied bonito, fresh or chilled"),
            H("43", "Tilapia, catfish, carp and eels, fresh or chilled"),
            H("44", "Milkfish (Chanos chanos), fresh or chilled"),
            H("45", "Other fish of freshwater, fresh or chilled"),
            H("71", "Cod (Gadus morhua, Gadus ogac, Gadus macrocephalus), fresh or chilled"),
            H("89", "Other fish, fresh or chilled"));

        yield return Cat("0303", "Fish, frozen", "BFAR", BfarSps,
            H("11", "Sockeye salmon (red salmon), frozen"),
            H("12", "Other Pacific salmon, frozen"),
            H("13", "Atlantic and Danube salmon, frozen"),
            H("31", "Tunas of the genus Thunnus, frozen"),
            H("41", "Tilapia (Oreochromis spp.), frozen"),
            H("43", "Tilapia, catfish, carp and eels, frozen"),
            H("44", "Milkfish (Chanos chanos), frozen"),
            H("63", "Fish of the families Scombridae (excluding tunas), frozen"),
            H("89", "Other fish, frozen"));

        yield return Cat("0304", "Fish fillets and other fish meat, fresh, chilled or frozen", "BFAR", BfarSps,
            H("31", "Tilapia, catfish, carp and eels fillets, fresh or chilled"),
            H("32", "Tilapia, catfish, carp and eels fillets, frozen"),
            H("41", "Tunas of genus Thunnus fillets, fresh or chilled"),
            H("42", "Tunas of genus Thunnus fillets, frozen"),
            H("89", "Other fish fillets and fish meat"));

        yield return Cat("0305", "Fish, dried, salted or in brine; smoked fish", "BFAR", BfarSps,
            H("31", "Tilapia, catfish, carp and eels, dried"),
            H("32", "Tilapia, catfish, carp and eels, salted or in brine"),
            H("39", "Other fish, dried"),
            H("43", "Other fish, salted or in brine"),
            H("51", "Smoked fish including fillets"),
            H("59", "Other smoked fish products"));

        yield return Cat("0306", "Crustaceans, live, fresh, chilled, frozen, dried, salted or smoked", "BFAR", BfarSps,
            H("11", "Rock lobster and other sea crawfish, live or fresh"),
            H("17", "Shrimps and prawns, frozen"),
            H("31", "Rock lobster and other sea crawfish, frozen"),
            H("33", "Crabs, frozen"),
            H("43", "Shrimps and prawns, dried"),
            H("63", "Crabs, live, fresh or chilled"),
            H("99", "Other crustaceans"));

        yield return Cat("0307", "Molluscs, live, fresh, chilled, frozen, dried, salted or smoked", "BFAR", BfarSps,
            H("11", "Oysters, live, fresh or chilled"),
            H("19", "Other oysters"),
            H("29", "Scallops, frozen"),
            H("42", "Squid, frozen"),
            H("43", "Cuttlefish, frozen"),
            H("60", "Snails (other than sea snails), live, fresh or chilled"),
            H("71", "Clams, cockles and ark shells, live, fresh or chilled"),
            H("81", "Abalone, live, fresh or chilled"),
            H("99", "Other molluscs"));

        yield return Cat("0308", "Aquatic invertebrates other than crustaceans and molluscs", "BFAR", BfarSps,
            H("11", "Sea cucumbers (Stichopus japonicus, Holothuroidea), live, fresh or chilled"),
            H("19", "Other sea cucumbers"),
            H("30", "Jellyfish (Rhopilema spp.), live, fresh or chilled"),
            H("90", "Other aquatic invertebrates"));

        yield return Cat("1604", "Prepared or preserved fish; caviar and caviar substitutes", "BFAR", BfarSps,
            H("11", "Salmon"),
            H("12", "Herrings"),
            H("13", "Sardines, sardinella and brisling or sprats"),
            H("14", "Tunas, skipjack and bonito (Sarda spp.)"),
            H("20", "Other prepared or preserved fish"),
            H("31", "Caviar"),
            H("32", "Caviar substitutes"));

        yield return Cat("1605", "Crustaceans, molluscs and other aquatic invertebrates, prepared or preserved", "BFAR", BfarSps,
            H("10", "Crab"),
            H("21", "Shrimps and prawns, not in airtight container"),
            H("29", "Other shrimps and prawns"),
            H("30", "Lobster"),
            H("40", "Other crustaceans"),
            H("51", "Oysters"),
            H("52", "Scallops, including queen scallops"),
            H("53", "Mussels"),
            H("54", "Cuttlefish and squid"),
            H("55", "Octopus"),
            H("90", "Other aquatic invertebrates"));
    }

    private static IEnumerable<MavHsLibrarySeedCategory> SraOfficialMav()
    {
        yield return Cat("1701", "Cane or beet sugar and chemically pure sucrose, in solid form", "SRA", SraMav,
            H("11", "Raw cane sugar, not containing added flavouring or colouring matter"),
            H("12", "Raw beet sugar, not containing added flavouring or colouring matter"),
            H("13", "Cane sugar specified in subheading note 2 to this chapter"),
            H("14", "Other cane sugar"),
            H("91", "Refined sugar containing added flavouring or colouring matter"),
            H("99", "Other cane or beet sugar and chemically pure sucrose"));
    }

    private static IEnumerable<MavHsLibrarySeedCategory> NtaTobaccoProducts()
    {
        yield return Cat("2401", "Unmanufactured tobacco; tobacco refuse", "NTA", NtaTobacco,
            H("10", "Tobacco, not stemmed/stripped"),
            H("20", "Tobacco, partly or wholly stemmed/stripped"),
            H("30", "Tobacco refuse"));

        yield return Cat("2402", "Cigars, cheroots, cigarillos and cigarettes, of tobacco or of tobacco substitutes", "NTA", NtaTobacco,
            H("10", "Cigars, cheroots and cigarillos, containing tobacco"),
            H("20", "Cigarettes containing tobacco"),
            H("90", "Other cigars, cheroots, cigarillos and cigarettes"));

        yield return Cat("2403", "Other manufactured tobacco and manufactured tobacco substitutes", "NTA", NtaTobacco,
            H("11", "Water pipe tobacco specified in subheading note 1 to this chapter"),
            H("19", "Other smoking tobacco"),
            H("91", "Homogenised or reconstituted tobacco"),
            H("99", "Other manufactured tobacco"));
    }

    private static MavHsLibrarySeedCategory Cat(
        string hsCode,
        string description,
        string agencyCode,
        string? notes,
        params MavHsLibrarySeedHeading[] headings) =>
        new(hsCode, description, agencyCode, notes, headings);

    private static MavHsLibrarySeedHeading H(string headingNumber, string description) =>
        new(headingNumber, description, new[] { new MavHsLibrarySeedDetail(description) });
}
