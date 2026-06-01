using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blabbermouth.Data;

public record DownloadableModel(
    string Name,
    string Url
)
{
    private static readonly HttpClient Client = new();

    public static string DownloadedModelsFolder =>
        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? Environment.CurrentDirectory,
            "downloaded-models");
    public static string TempFolder =>
        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? Environment.CurrentDirectory,
            "temp");
    public string DirectoryPath => Path.Combine(DownloadedModelsFolder, Name);
    public bool IsPresent => Path.Exists(DirectoryPath);
    
    public async Task DownloadAndExtractAsync(Action<float> progressCallback)
    {
        bool isZip = Url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        bool isTarBz2 = Url.EndsWith(".tar.bz2", StringComparison.OrdinalIgnoreCase);
        
        using HttpResponseMessage response = await Client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        
        long totalBytes = response.Content.Headers.ContentLength ?? -1;
        
        await using Stream contentStream = await response.Content.ReadAsStreamAsync();
        await using FileStream fileStream = new(Path.Combine(TempFolder, 
                isZip || isTarBz2 
                    ? $"{Name}.{(isZip ? "zip" : "tar.bz2")}"
                    : "Microsoft.VisualStudio.Services.VSIXPackage"
            ),
            FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        
        long downloadedBytes = 0;
        
        byte[] buffer = new byte[8192];
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            downloadedBytes += bytesRead;
            if (totalBytes <= 0) continue;
            
            float progress = (float)downloadedBytes / totalBytes;
            progressCallback(progress);
        }
        
        fileStream.Close();
        
        if (isZip)
        {
            await ZipFile.ExtractToDirectoryAsync(fileStream.Name, DownloadedModelsFolder);
        }
        else if (isTarBz2)
        {
            ProcessStartInfo psi = new()
            {
                FileName = "tar",
                Arguments = $"-xjf \"{fileStream.Name}\" -C \"{DownloadedModelsFolder}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            using Process process = Process.Start(psi)!;
            
            string stderr = await process.StandardError.ReadToEndAsync();
            string stdout = await process.StandardOutput.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
            {
                throw new($"Extraction failed with code {process.ExitCode}.\nStdout: {stdout}\nStderr: {stderr}");
            }
        }
        else
        {
            string entryPrefix = $"extension/assets/stt/{Name}/";
            Directory.CreateDirectory(DirectoryPath);
            
            await using FileStream zipStream = File.OpenRead(fileStream.Name);
            await using ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
            
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!entry.FullName.StartsWith(entryPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                
                string relativePath = entry.FullName[entryPrefix.Length..];
                string destinationPath = Path.Combine(DirectoryPath, relativePath);
                
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                
                await entry.ExtractToFileAsync(destinationPath, true);
            }
        }
        File.Delete(fileStream.Name);
    }
}