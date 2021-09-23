# Detroit Become Human file parser

Reads the game's files and extract the text contents to json files.

## Usage

### Help

Shows tool usage help.

Examples:  
- `FileParser -h`

### Extract

Extracts the translation keys and their value and generate a json file for each language.

|Option|Usage|Description|Required|
|:--|:--:|:--|:--:|
|Input|-i, --input|The path to the folder containing the BigFile_PC.d* files. Default value is `./input`.|No|
|Output|-o, --output|The path to the folder where the json files will be exported. Default value is `./output`.|No|
|Verbosity|-v, --verbose|If set, the program will show the extraction progress.|No|

Examples:  
`FileParser extract -v -i ./input`  
`FileParser extract --input C:/Foo/Bar --output ./output`

## Search

Search for a value in the files.

|Option|Usage|Description|Required|
|:--|:--:|:--|:--:|
|Value||The value to search (case sensitive).|Yes|
|In keys|-k, --in-keys|If set, will search the value in the translation keys. Otherwise, in the translation values. Default value is `false`.|No|
|Buffer size|-b, --buffer-size|Size of the buffer used to process the files, in **bytes**. Default value is 100 Mb.|No|
|Verbosity|-v, --verbose|If set, the program will show the extraction progress.|No|

Examples:  
`FileParser search Gourami --verbose -b 1024`  