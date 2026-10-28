using System.Globalization;

Console.WriteLine("File Count Reporter");
Console.WriteLine("Enter a path relative to your user folder, or an absolute path. Press Enter to exit.");

while (true)
{
	Console.Write("Folder: ");
	var folderPath = ResolveFolderPath(Console.ReadLine());

	if (folderPath is null)
	{
		break;
	}

	if (!Directory.Exists(folderPath))
	{
		Console.WriteLine("That folder does not exist. Please try again.");
		continue;
	}

	folderPath = SelectSubfolder(folderPath);

	var fileCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	var filesSkipped = 0;

	CountFiles(folderPath, fileCounts, ref filesSkipped);

	Console.WriteLine();
	Console.WriteLine($"File counts for: {Path.GetFullPath(folderPath)}");
	Console.WriteLine(new string('-', 36));

	if (fileCounts.Count == 0)
	{
		Console.WriteLine("No files were found.");
	}
	else
	{
		foreach (var fileCount in fileCounts
					 .OrderByDescending(item => item.Value)
					 .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
		{
			Console.WriteLine($"{fileCount.Key,-20} {fileCount.Value,8}");
		}

		Console.WriteLine(new string('-', 36));
		Console.WriteLine($"Total files: {fileCounts.Values.Sum()}");
	}

	if (filesSkipped > 0)
	{
		Console.WriteLine($"Files or folders skipped because they could not be read: {filesSkipped}");
	}

	Console.WriteLine();
}


/// <summary>
/// Resolves the full path of a folder based on the specified path.
/// </summary>
/// <param name="folderPath">The path of the folder to resolve.</param>
/// <returns>The full path of the folder, or null if the path is invalid.</returns>
static string? ResolveFolderPath(string? folderPath)
{
	if (string.IsNullOrWhiteSpace(folderPath))
	{
		return null;
	}

	folderPath = CapitalizePathSegments(folderPath.Trim().Trim('"'));

	if (Path.IsPathFullyQualified(folderPath))
	{
		return folderPath;
	}

	var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
	return Path.Combine(userProfilePath, folderPath);
}


/// <summary>
/// Capitalizes the first letter of each segment in the specified path.
/// </summary>
/// <param name="path">The path to capitalize.</param>
/// <returns>The path with capitalized segments.</returns>
static string CapitalizePathSegments(string path)
{
	var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

	for (var index = 0; index < segments.Length; index++)
	{
		if (segments[index].Length > 0)
		{
			segments[index] = char.ToUpperInvariant(segments[index][0]) + segments[index][1..];
		}
	}

	return string.Join(Path.DirectorySeparatorChar, segments);
}


/// <summary>
/// Allows the user to select a subfolder from the specified folder.
/// </summary>
/// <param name="folderPath">The path of the folder to select a subfolder from.</param>
/// <returns>The path of the selected subfolder.</returns>
static string SelectSubfolder(string folderPath)
{
	while (true)
	{
		Console.WriteLine();
		Console.WriteLine("What would you like to count?");
		Console.WriteLine("1. This folder");
		Console.WriteLine("2. A subfolder");
		Console.Write("Select an option: ");

		var option = Console.ReadLine();

		if (option == "1")
		{
			return folderPath;
		}

		if (option == "2")
		{
			break;
		}

		Console.WriteLine("Please enter 1 or 2.");
	}

	var subfolders = new List<string>();

	try
	{
		subfolders = Directory.EnumerateDirectories(folderPath)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}
	catch (UnauthorizedAccessException)
	{
		Console.WriteLine("The subfolders could not be read. The selected folder will be used.");
		return folderPath;
	}
	catch (IOException)
	{
		Console.WriteLine("The subfolders could not be read. The selected folder will be used.");
		return folderPath;
	}

	if (subfolders.Count == 0)
	{
		return folderPath;
	}

	Console.WriteLine();
	Console.WriteLine($"Subfolders in {Path.GetFullPath(folderPath)}:");

	for (var index = 0; index < subfolders.Count; index++)
	{
		Console.WriteLine($"{index + 1}. {Path.GetFileName(subfolders[index])}");
	}

	while (true)
	{
		Console.Write("Select a subfolder number: ");
		var selection = Console.ReadLine();

		if (int.TryParse(selection, out var selectedNumber)
			&& selectedNumber >= 1
			&& selectedNumber <= subfolders.Count)
		{
			return subfolders[selectedNumber - 1];
		}

		Console.WriteLine("Please enter one of the displayed numbers.");
	}
}

/// <summary>
/// Counts the files in the specified folder and its subfolders.
/// </summary>
/// <param name="folderPath">The path of the folder to count files in.</param>
/// <param name="fileCounts">A dictionary to store the counts of each file type.</param>
/// <param name="filesSkipped">A reference to an integer that tracks the number of files that could not be accessed.</param>
static void CountFiles(string folderPath, Dictionary<string, int> fileCounts, ref int filesSkipped)
{
	try
	{
		foreach (var filePath in Directory.EnumerateFiles(folderPath))
		{
			var extension = Path.GetExtension(filePath);
			var fileType = string.IsNullOrEmpty(extension)
				? "[no extension]"
				: extension.ToLower(CultureInfo.InvariantCulture);

			fileCounts[fileType] = fileCounts.GetValueOrDefault(fileType) + 1;
		}
	}
	catch (UnauthorizedAccessException)
	{
		filesSkipped++;
	}
	catch (IOException)
	{
		filesSkipped++;
	}

	try
	{
		foreach (var subdirectory in Directory.EnumerateDirectories(folderPath))
		{
			CountFiles(subdirectory, fileCounts, ref filesSkipped);
		}
	}
	catch (UnauthorizedAccessException)
	{
		filesSkipped++;
	}
	catch (IOException)
	{
		filesSkipped++;
	}
}
