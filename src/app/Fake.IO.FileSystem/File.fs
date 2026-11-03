namespace Fake.IO

open System.Text
open System.IO
open Operators

/// <namespacedoc>
/// <summary>
/// IO namespace contains tasks to interact file system
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Contains filters for files
/// </summary>
[<RequireQualifiedAccess>]
module FileFilter =
    let allFiles _ = true

/// <summary>
/// Contains helpers which allow to interact with the file system.
/// </summary>
[<RequireQualifiedAccess>]
module File =
    /// see https://stackoverflow.com/questions/2502990/create-text-file-without-bom
    let internal utf8WithoutBom = UTF8Encoding(false)

    /// <summary>
    /// Detect the encoding, from https://stackoverflow.com/questions/3825390/effective-way-to-find-any-files-encoding
    /// Detect the encoding
    /// </summary>
    ///
    /// <param name="def">The encoding to detect for</param>
    /// <param name="filename">The file name to check</param>
    let getEncoding def filename =
        // Read the BOM
        let bom = Array.zeroCreate 4

        let read =
            use file = new FileStream(filename, FileMode.Open, FileAccess.Read)
            file.Read(bom, 0, 4)

        match bom |> Array.toList with
        | _ when read < 2 -> def
        | 0xffuy :: 0xfeuy :: _ -> Encoding.Unicode //UTF-16LE
        | 0xfeuy :: 0xffuy :: _ -> Encoding.BigEndianUnicode //UTF-16BE
        | _ when read < 3 -> def
        | 0xefuy :: 0xbbuy :: 0xbfuy :: _ -> Encoding.UTF8
        | _ when read < 4 -> def
        | 0uy :: 0uy :: 0xfeuy :: 0xffuy :: _ -> Encoding.UTF32
        | _ -> def


    /// <summary>
    /// Checks if the file exists on disk.
    /// </summary>
    ///
    /// <param name="fileName">The file name to check</param>
    let exists fileName = File.Exists fileName

    /// <summary>
    /// Gets the encoding from the file or the default of the file doesn't exist
    /// </summary>
    ///
    /// <param name="def">The encoding to detect for</param>
    /// <param name="filename">The file name to check</param>
    let getEncodingOrDefault def filename =
        if not (exists filename) then
            def
        else
            getEncoding def filename

    /// <summary>
    /// Get the encoding from the file or utf8 without BOM if unknown or the file doesn't exist
    /// </summary>
    let getEncodingOrUtf8WithoutBom = getEncodingOrDefault utf8WithoutBom

    /// <summary>
    /// Raises an exception if the file doesn't exist on disk.
    /// </summary>
    ///
    /// <param name="filename">The file name to check</param>
    let checkExists fileName =
        if not <| exists fileName then
            FileNotFoundException(sprintf "File %s does not exist." fileName) |> raise

    /// <summary>
    /// Checks if all given files exist.
    /// </summary>
    ///
    /// <param name="files">The files names to check</param>
    let allExist files = Seq.forall File.Exists files

    /// <summary>
    /// Tries to get the version of a file. Throws `FileNotFoundException` if the file doesn't exist.
    /// Returns None if the file doesn't contain a `FileVersion` component.
    /// On non-windows platforms this API returns assembly metadata instead,
    /// see <a href="https://github.com/dotnet/corefx/blob/5fb98a118bb19a91e8ffb5c17ff5e7c00a4c05ee/src/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L20-L28">FileVersionInfo.Unix.cs</a>
    /// </summary>
    ///
    /// <param name="fileName">Name of file from which the version is retrieved. The path can be relative.</param>
    let tryGetVersion (fileName: string) : string option =
        Path.getFullName fileName
        |> System.Diagnostics.FileVersionInfo.GetVersionInfo
        |> fun x -> if isNull x.FileVersion then None else Some x.FileVersion

    /// <summary>
    /// Get the version a file. This overload throws when the file has no version,
    /// consider using `tryGetVersion` instead. On non-windows platforms this API returns assembly metadata instead,
    /// <a href="see https://github.com/dotnet/corefx/blob/5fb98a118bb19a91e8ffb5c17ff5e7c00a4c05ee/src/System.Diagnostics.FileVersionInfo/src/System/Diagnostics/FileVersionInfo.Unix.cs#L20-L28">FileVersionInfo.Unix.cs</a>
    /// </summary>
    ///
    /// <param name="fileName">Name of file from which the version is retrieved. The path can be relative.</param>
    let getVersion (fileName: string) : string =
        match tryGetVersion fileName with
        | Some v -> v
        | None ->
            raise
            <| System.InvalidOperationException(sprintf "The file '%s' doesn't contain a file version" fileName)

    /// <summary>
    /// Creates a file if it does not exist.
    /// </summary>
    ///
    /// <param name="fileName">The name of the file to create</param>
    let create fileName =
        let file = FileInfo.ofPath fileName

        if not file.Exists then
            file.Create().Dispose()

    /// <summary>
    /// Deletes a file if it exists.
    /// </summary>
    ///
    /// <param name="fileName">The name of the file to delete</param>
    let delete fileName =
        let file = FileInfo.ofPath fileName

        if file.Exists then
            file.Delete()

    /// <summary>
    /// Deletes the given files.
    /// </summary>
    ///
    /// <param name="files">The name of the files to delete</param>
    let deleteAll files = Seq.iter delete files

    /// <summary>
    /// Active Pattern for determining file extension.
    /// </summary>
    ///
    /// <param name="extension">The extension to look for</param>
    /// <param name="file">The file name to use</param>
    let (|EndsWith|_|) (extension: string) (file: string) =
        if file.EndsWith extension then Some() else None

    /// <summary>
    /// Reads a file line by line
    /// </summary>
    ///
    /// <param name="encoding">The encoding to use</param>
    /// <param name="file">The file name to use</param>
    let readWithEncoding (encoding: Encoding) (file: string) =
        seq {
            use stream = File.OpenRead(file)
            use textReader = new StreamReader(stream, encoding)

            while not textReader.EndOfStream do
                yield textReader.ReadLine()
        }

    let read (file: string) =
        readWithEncoding (getEncodingOrUtf8WithoutBom file) file

    /// <summary>
    /// Reads the first line of a file. This can be helpful to read a password from file.
    /// </summary>
    ///
    /// <param name="encoding">The encoding to use</param>
    /// <param name="file">The file name to use</param>
    let readLineWithEncoding (encoding: Encoding) (file: string) =
        use stream = File.OpenRead file
        use sr = new StreamReader(stream, encoding)
        sr.ReadLine()

    /// <summary>
    /// Reads the first line of a file. This can be helpful to read a password from file.
    /// </summary>
    ///
    /// <param name="file">The file name to use</param>
    let readLine (file: string) =
        readLineWithEncoding (getEncodingOrUtf8WithoutBom file) file

    /// <summary>
    /// Writes a file line by line
    /// </summary>
    ///
    /// <param name="encoding">The encoding to use</param>
    /// <param name="append">Flag to check if to append content or overwrite</param>
    /// <param name="filename">The file name to use</param>
    /// <param name="lines">The lines to write</param>
    let writeWithEncoding (encoding: Encoding) append fileName (lines: seq<string>) =
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)
        use writer = new StreamWriter(file, encoding)
        lines |> Seq.iter writer.WriteLine

    /// <summary>
    /// Write the given sequence of lines to the file. Either append to the end of the file or overwrite
    /// </summary>
    ///
    /// <param name="append">Flag to check if to append content or overwrite</param>
    /// <param name="filename">The file name to use</param>
    /// <param name="lines">The lines to write</param>
    let write append fileName (lines: seq<string>) =
        writeWithEncoding (getEncodingOrUtf8WithoutBom fileName) append fileName lines

    /// <summary>
    /// Writes a byte array to a file
    /// </summary>
    ///
    /// <param name="file">The file name to use</param>
    /// <param name="bytes">The bytes to write</param>
    let writeBytes file (bytes: byte array) = File.WriteAllBytes(file, bytes)

    /// <summary>
    /// Writes a string to a file
    /// </summary>
    ///
    /// <param name="encoding">The encoding to use</param>
    /// <param name="append">Flag to check if to append content or overwrite</param>
    /// <param name="filename">The file name to use</param>
    /// <param name="text">The string text to write</param>
    let writeStringWithEncoding (encoding: Encoding) append fileName (text: string) =
        let fi = FileInfo.ofPath fileName
        use file = fi.Open(if append then FileMode.Append else FileMode.Create)

        (use writer = new StreamWriter(file, encoding)
         writer.Write text)

        file.Close()

    /// <summary>
    /// Writes a string to a file
    /// </summary>
    ///
    /// <param name="append">Flag to check if to append content or overwrite</param>
    /// <param name="filename">The file name to use</param>
    /// <param name="text">The string text to write</param>
    let writeString append fileName (text: string) =
        writeStringWithEncoding (getEncodingOrUtf8WithoutBom fileName) append fileName text

    /// <summary>
    /// Replaces the file with the given string
    /// </summary>
    ///
    /// <param name="filename">The file name to use</param>
    /// <param name="text">The string text to write</param>
    let replaceContent fileName text =
        let fi = FileInfo.ofPath fileName

        if fi.Exists then
            fi.IsReadOnly <- false
            fi.Delete()

        writeString false fileName text

    /// <summary>
    /// Writes a file line by line
    /// </summary>
    ///
    /// <param name="file">The file name to use</param>
    /// <param name="lines">The lines to write</param>
    let writeNew file lines = write false file lines

    /// <summary>
    /// Appends all lines to a file line by line
    /// </summary>
    ///
    /// <param name="file">The file name to use</param>
    /// <param name="lines">The lines to append</param>
    let append file lines = write true file lines

    /// <summary>
    /// Reads a file as one text
    /// </summary>
    ///
    /// <param name="encoding">The encoding to use</param>
    /// <param name="file">The file name to use</param>
    let inline readAsStringWithEncoding encoding file = File.ReadAllText(file, encoding)

    /// <summary>
    /// Reads a file as one text
    /// </summary>
    ///
    /// <param name="file">The file name to use</param>
    let inline readAsString file =
        File.ReadAllText(file, (getEncodingOrUtf8WithoutBom file))

    /// <summary>
    /// Reads a file as one array of bytes
    /// </summary>
    ///
    /// <param name="file">The file name to use</param>
    let readAsBytes file = File.ReadAllBytes file

    /// <summary>
    /// Replaces the text in the given file
    /// </summary>
    ///
    /// <param name="replaceF">The callback to execute when replacing content</param>
    /// <param name="fileName">The file name to use</param>
    let applyReplace replaceF fileName =
        fileName |> readAsString |> replaceF |> replaceContent fileName
