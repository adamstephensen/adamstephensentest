﻿namespace Dtos;

public class FileUploadsDto
{
    public string Index { get; set; }
    public string Folder { get; set; }
    public IFormFileCollection Files { get; set; }
}