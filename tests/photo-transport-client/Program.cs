using PlantCare.PhotoTransport;

// Synthetic integration harness only; never included in the API or decoder.
try
{
    using var input = new MemoryStream();
    await Console.OpenStandardInput().CopyToAsync(input);
    if (input.Length > UnixPhotoProcessor.MaximumInputBytes) return 2;
    var client = new UnixPhotoProcessor(new(true, args[0], uint.Parse(args[1])));
    using var cancel = new CancellationTokenSource();
    if (args.Length > 2 && args[2] == "cancel") cancel.CancelAfter(100);
    var result = await client.NormalizeAsync(input.ToArray(), "image/png", cancel.Token);
    Console.WriteLine($"{result.Width}x{result.Height}");
    return 0;
}
catch (PhotoProcessingException error) { Console.Error.WriteLine(error.Code); return 2; }
catch (OperationCanceledException) { Console.Error.WriteLine("photo-test-canceled"); return 3; }
