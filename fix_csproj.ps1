$projectPath = "C:\Users\Jan.Masopoga\source\repos\Municipal App\Municipal App.csproj"
$basePath    = "C:\Users\Jan.Masopoga\source\repos\Municipal App"

$filesToAdd = @(
	"$basePath\MainMenuForm.cs",
	"$basePath\ReportIssueForm.cs",
	"$basePath\Models\IssueReport.cs",
	"$basePath\ViewReportsForm.cs"
)

try {
	$dte = [System.Runtime.InteropServices.Marshal]::GetActiveObject("VisualStudio.DTE")

	$project = $null
	foreach ($p in $dte.Solution.Projects) {
		if ($p.FullName -eq $projectPath) { $project = $p; break }
	}

	if ($project -eq $null) { Write-Host "ERROR: Project not found"; exit 1 }

	foreach ($file in $filesToAdd) {
		$already = $false
		foreach ($item in $project.ProjectItems) {
			if ($item.FileNames(1) -eq $file) { $already = $true; break }
		}
		if (-not $already) {
			$project.ProjectItems.AddFromFile($file) | Out-Null
			Write-Host "Added: $file"
		} else {
			Write-Host "Already present: $file"
		}
	}

	$project.Save()
	Write-Host "Project saved"
}
catch {
	Write-Host "DTE error: $_"
	exit 1
}
