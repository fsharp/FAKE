# SignTool

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later. The old documentation can be found <a href="apidocs/v4/fake-signtoolhelper.html">here</a>.</p>
</div>


This module is a wrapper around the [signtool.exe](https://docs.microsoft.com/en-gb/windows/win32/seccrypto/signtool) tool, a command-line tool that digitally signs files, verifies signatures in files, or time stamps files.

The 3 supported functions are:

 - [SignTool.sign: digitally signing files](#Signing)
 - [SignTool.timeStamp: time stamping previously signed files](#Time-stamping)
 - [SignTool.verify: verify signed files](#Verifying)

Additional information:

 - [Common options: options common to all supported functions](#Common-options)
 - [Certificates: notes and how to get one](#Certificates)
 - [SHA1/SHA256: differences and when to use which](#SHA1-SHA256)

API Reference:

 - [`SignTool`](apidocs/v5/fake-tools-signtool.html): The SignTool tool is a command-line tool that digitally signs files, verifies signatures in files, or time stamps files.
 - [`CertificateFromFile`](apidocs/v5/fake-tools-signtool-certificatefromfile.html): Specifies parameters to use when using a certificate from a file.
 - [`CertificateFromStore`](apidocs/v5/fake-tools-signtool-certificatefromstore.html): Specifies parameters to use when using a certificate from a certificate store.
 - [`SignCertificate`](apidocs/v5/fake-tools-signtool-signcertificate.html): Specifies what type of certificate to use.
 - [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html): Sign command options
 - [`TimeStampOption`](apidocs/v5/fake-tools-signtool-timestampoption.html): Specifies the URL of the time stamp server and the digest algorithm used by the RFC 3161 time stamp server.
 - [`TimeStampOptions`](apidocs/v5/fake-tools-signtool-timestampoptions.html): Timestamp command options
 - [`VerifyOptions`](apidocs/v5/fake-tools-signtool-verifyoptions.html): Verify command options

<hr /><hr />

## Open namespace

```fsharp
open Fake.Tools
```

<hr /><hr />

## Signing

Digitally signing files.

A [certificate](#Certificates) is needed to do this.


### When the certificate is located in a .pfx file

Only PFX files are supported by signtool.exe.

```fsharp
// val sign :
//  certificate:SignTool.SignCertificate
//  -> setOptions:(SignTool.SignOptions -> SignTool.SignOptions)
//  -> files:seq<string>
//  -> unit
SignTool.sign
    (SignTool.SignCertificate.FromFile(
        "path/to/certificate-file.pfx",
        fun o -> { o with
                    Password = Some "certificate-password" } ) )
    (fun o -> o)
    ["program.exe"; "library.dll"]
```

Only a subset of options is shown in the example, see API Reference for all available options: [`CertificateFromFile`](apidocs/v5/fake-tools-signtool-certificatefromfile.html), [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html).

### When the certificate is located in a certificate store

All options are optional, and any combination may be used, depending on specific needs.

If no `StoreName` is specified, the "My" store is opened.

```fsharp
// val sign :
//  certificate:SignTool.SignCertificate
//  -> setOptions:(SignTool.SignOptions -> SignTool.SignOptions)
//  -> files:seq<string>
//  -> unit
SignTool.sign
    (SignTool.SignCertificate.FromStore(
        fun o -> { o with
                    AutomaticallySelectCertificate = Some true
                    SubjectName = Some "subject"
                    StoreName = Some "My" } ) )
    (fun o -> o)
    ["program.exe"; "library.dll"]
```

Only a subset of options is shown in the example, see API Reference for all available options: [`CertificateFromStore`](apidocs/v5/fake-tools-signtool-certificatefromstore.html), [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html).

### Custom signing options

Use SHA256 ([see SHA1/SHA256](#SHA1-SHA256)) to create file signatures.

```fsharp
// val sign :
//  certificate:SignTool.SignCertificate
//  -> setOptions:(SignTool.SignOptions -> SignTool.SignOptions)
//  -> files:seq<string>
//  -> unit
SignTool.sign
    (SignTool.SignCertificate.From..(..))
    (fun o -> { o with
                    DigestAlgorithm = Some SignTool.DigestAlgorithm.SHA256 } )
    ["program.exe"; "library.dll"]
```

Only a subset of options is shown in the example, see API Reference for all available options: [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html).

### Adding a time stamp

Time stamp at the same time as signing.

There is a separate function `signWithTimeStamp` that, compared to `sign`, has 2 additional parameters to set time stamping options.

If you want to time stamp previously signed files, use the [Time stamping](#Time-stamping) function.

For more information about time stamping [see Time stamping](#Time-stamping).

```fsharp
// val signWithTimeStamp :
//  certificate:SignTool.SignCertificate
//  -> setSignOptions:(SignTool.SignOptions -> SignTool.SignOptions)
//  -> serverUrl:string
//  -> setTimeStampOptions:(SignTool.TimeStampOption -> SignTool.TimeStampOption)
//  -> files:seq<string>
//  -> unit
SignTool.signWithTimeStamp
    (SignTool.SignCertificate.From..(..))
    (fun o -> o)
    "http://timestamp.example-ca.com"
    (fun o -> o)
    ["program.exe"; "library.dll"]
```

Only a subset of options is shown in the example, see API Reference for all available options: [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html), [`TimeStampOption`](apidocs/v5/fake-tools-signtool-timestampoption.html).

#### Custom time stamp options

Use SHA256 ([see SHA1/SHA256](#SHA1-SHA256)).

```fsharp
// val signWithTimeStamp :
//  certificate:SignTool.SignCertificate
//  -> setSignOptions:(SignTool.SignOptions -> SignTool.SignOptions)
//  -> serverUrl:string
//  -> setTimeStampOptions:(SignTool.TimeStampOption -> SignTool.TimeStampOption)
//  -> files:seq<string>
//  -> unit
SignTool.signWithTimeStamp
    (SignTool.SignCertificate.From..(..))
    (fun o -> o)
    "http://timestamp.example-ca.com"
    (fun o -> { o with
                    Algorithm = Some SignTool.DigestAlgorithm.SHA256 } )
    ["program.exe"; "library.dll"]
```

Only a subset of options is shown in the example, see API Reference for all available options: [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html), [`TimeStampOption`](apidocs/v5/fake-tools-signtool-timestampoption.html).

<hr /><hr />

## Time stamping

Time stamping previously signed files.

When signing a file, the signature is valid only as long as the certificate used to create it is valid. The moment the certificate expires, the signature becomes invalid.
Time stamping is used to extend the validity of the signature. A time stamp proves that the signature was created while the certificate was still valid and effectively extends the signature's validity indefinitely.


### Default options

Time stamp server does not have to be from the same CA as the certificate.

```fsharp
// val timeStamp :
//  serverUrl:string
//  -> setOptions:(SignTool.TimeStampOptions -> SignTool.TimeStampOptions)
//  -> files:seq<string>
//  -> unit
SignTool.timeStamp
    "http://timestamp.example-ca.com"
    (fun o -> o)
    ["program.exe"; "library.dll"]
```

Only a subset of options is shown in the example, see API Reference for all available options: [`TimeStampOptions`](apidocs/v5/fake-tools-signtool-timestampoptions.html).

### Custom options

Use SHA256 ([see SHA1/SHA256](#SHA1-SHA256)).

```fsharp
// val timeStamp :
//  serverUrl:string
//  -> setOptions:(SignTool.TimeStampOptions -> SignTool.TimeStampOptions)
//  -> files:seq<string>
//  -> unit
SignTool.timeStamp
    "http://timestamp.example-ca.com"
    (fun o -> { o with
                    Algorithm = Some SignTool.DigestAlgorithm.SHA256 } )
    ["program.exe"; "library.dll"]
```

Only a subset of options is shown in the example, see API Reference for all available options: [`TimeStampOption`](apidocs/v5/fake-tools-signtool-timestampoption.html), [`TimeStampOptions`](apidocs/v5/fake-tools-signtool-timestampoptions.html).

<hr /><hr />

## Verifying

Verify signed files.

The verify command determines whether the signing certificate was issued by a trusted authority, whether the signing certificate has been revoked, and, optionally, whether the signing certificate is valid for a specific policy.


### Default options

```fsharp
// val verify :
//  setOptions:(SignTool.VerifyOptions -> SignTool.VerifyOptions)
//  -> files:seq<string>
//  -> unit
SignTool.verify
    (fun o -> o)
    ["program.exe"; "library.dll"]
```

Only a subset of options is shown in the example, see API Reference for all available options: [`VerifyOptions`](apidocs/v5/fake-tools-signtool-verifyoptions.html).


### Custom options

```fsharp
// val verify :
//  setOptions:(SignTool.VerifyOptions -> SignTool.VerifyOptions)
//  -> files:seq<string>
//  -> unit
SignTool.verify
    (fun o ->
        { o with
            AllSignatures = Some true
            RootSubjectName = Some "root subject"
            WarnIfNotTimeStamped = Some true } )
    ["program.exe"; "library.dll"]
```

Only a subset of options is shown in the example, see API Reference for all available options: [`VerifyOptions`](apidocs/v5/fake-tools-signtool-verifyoptions.html).

<hr /><hr />

## Common options

All functions share some common options.


Tool options - path to signtool.exe, execution timeout, working directory. These options are not set by default.

```fsharp
// set path to signtool.exe - if you want to use a specific version or you don't have Windows SDKs installed
// by default, an attempt will be made to locate it automatically in 'Program Files (x86)\Windows Kits'
{ o with
    ToolPath = Some "path/to/signtool.exe" }
// set the timeout
{ o with
    Timeout = Some (TimeSpan.FromMinutes 1.0) }
// set the working directory - uses current directory by default
{ o with
    WorkingDir = Some (Directory.GetCurrentDirectory()) }
```

Debug - displays debugging information (signtool option: /debug). This option is not set by default.

```fsharp
// display debugging information (/debug)
{ o with
    Debug = Some true }
// do not display debugging information
{ o with
    Debug = Some false }
// use default
{ o with
    Debug = None }
```

Verbosity - output verbosity (signtool options: /q, /v). This option is not set by default.

```fsharp
// set verbosity to verbose (/v)
{ o with
    Verbosity = Some SignTool.Verbosity.Verbose }
// set verbosity to quiet (/q)
{ o with
    Verbosity = Some SignTool.Verbosity.Quiet }
// use default
{ o with
    Verbosity = None }
```

API Reference: [`SignOptions`](apidocs/v5/fake-tools-signtool-signoptions.html), [`TimeStampOptions`](apidocs/v5/fake-tools-signtool-timestampoptions.html), [`VerifyOptions`](apidocs/v5/fake-tools-signtool-verifyoptions.html), [`Verbosity`](apidocs/v5/fake-tools-signtool-verbosity.html).

<hr /><hr />

## Certificates

The SignTool needs a certificate to sign files.


### Prod / release

For production / release purposes a proper publically trusted code signing certificate may be purchased from many CA's.


### Dev / test

For dev and testing purposes a certificate can be created using the [`New-SelfSignedCertificate` PowerShell cmdlet](https://docs.microsoft.com/en-us/powershell/module/pkiclient/new-selfsignedcertificate):
```powershell
New-SelfSignedCertificate -CertStoreLocation cert:\currentuser\my `
-Subject "CN=My Company, Inc.;O=My Company, Inc.;L=My City;C=SK" `
-KeyAlgorithm RSA `
-KeyLength 2048 `
-Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
-KeyExportPolicy Exportable `
-KeyUsage DigitalSignature `
-Type CodeSigningCert
```
This creates the certificate under "Certificates - Current User" -> "Personal" -> "Certificates" and prints the certificate Thumbprint. The certificate can be used as-is using the [`CertificateFromStore`](#When-the-certificate-is-located-in-a-certificate-store) option.

If you want to export the certificate to a file, use the [`Export-PfxCertificate` PowerShell cmdlet](https://docs.microsoft.com/en-us/powershell/module/pkiclient/export-pfxcertificate). Replace "{Thumbprint}" with the value from `New-SelfSignedCertificate` output:
```powershell
$certpwd = ConvertTo-SecureString -String "mycertpassword" -Force -AsPlainText
Get-ChildItem -Path cert:\currentuser\my\{Thumbprint} | Export-PfxCertificate -FilePath C:\certificate.pfx -Password $certpwd
```
Now the certificate can be used with the [`CertificateFromFile`](#When-the-certificate-is-located-in-a-pfx-file) option.

This certificate should not be used for prod / release purposes as it is self-signed and not trusted.

<hr /><hr />

## SHA1/SHA256

If the signed binaries are run on Windows 7 or newer, using SHA256 only is fine - this is also the default value for `DigestAlgorithm` (/fd and /td options).

If the signed binaries are run on Windows older than Windows 7, SHA1 should be used.

If the signed binaries are run on newer and older versions of Windows, then dual signing is probably the way to go. This means signing all binaries twice - first using SHA1, and then SHA256. Make sure to set `AppendSignature` to true when signing the second time, otherwise the first signature will be replaced. [More information about dual signing](https://support.ksoftware.net/support/solutions/articles/215805-the-truth-about-sha1-sha-256-dual-signing-and-code-signing-certificates-).
