# W3SavegameEditor
A tool for unpacking and packing savegames of the game The Witcher 3: Wild Hunt.   
 
## Usage

``` cs
//Read
SavegameFile saveGame = SavegameFile.Read(@"C:\Users\admin\Documents\The Witcher 3\gamesaves\ManualSave_1004de_7e923000_4df6d51.sav");

//Get collected cards
var cardCollection = ((BsVariable)((BsVariable)saveGame.Variables.Single(v => v.Name == "CR4GwintManager"))
    .Variables
    .Single(v => v.Name == "SBCollectionCardSize"))
    .Variables
    .Where(v => v is BsVariable vBs && vBs.Name == "SBCollectionCard")
    .Cast<BsVariable>()
    .Select(v => (v.Variables.Single(v => v.Name == "cardIndex"), v.Variables.Single(v => v.Name == "numCopies")))
    .ToArray();

//Uncheck DLCs
List<string> dlcNamesExclude = new List<string>() { "bob_000_000", "ep1", "dlc_011_001" };

var additionalContent = (BsVariable)(BsVariable)saveGame.Variables.Single(v => v.Name == "AdditionalContent");
var additionalContentCount = (VlVariable)additionalContent.Variables[0];
var additionalContentCountValue = (VariableValue<uint>)additionalContentCount.Value;
var excludedVariables = additionalContent.Variables.Where(v => v is VlVariable vlVariable
    && vlVariable.Type == "CName"
    && dlcNamesExclude.Contains((string)vlVariable.Value.Object));
foreach (var variable in excludedVariables)
    variable.Removed = true;
additionalContent.Variables = additionalContent.Variables.Where(v => !(excludedVariables.Contains(v))).ToArray();
additionalContentCountValue.Value = (uint)additionalContent.Variables.Count() - 1;

//Write
SavegameFile.Write(saveGame, @"C:\Users\admin\Documents\The Witcher 3\gamesaves\ManualSave_1004de_7e923000_4df6d51 - modified.sav");
```

Requirements
--------
[Microsoft .NET Framework 4.7.1][0]

Dependencies
--------
[K4os.Compression.LZ4][1]

[0]:https://dotnet.microsoft.com/download/dotnet-framework/net471
[1]:https://github.com/MiloszKrajewski/K4os.Compression.LZ4
