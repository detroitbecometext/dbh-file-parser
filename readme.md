# Detroit Become Human file parser

![build & test workflow](https://github.com/detroitbecometext/dbh-file-parser/actions/workflows/dotnet.yml/badge.svg)

Reads the game's files and extract the text contents to json files.

The tool extracts the text contents, remove some formattings tags, and creates one output file for each language containing the list of translation keys and their value.

Alternatively, to get a list of all values unprocessed in a single file, you can run the `unpacker.py` script in the `scripts` folder with Python.

> [!NOTE]
> The tool has only been tested with the Steam version of the game.

## Usage

### Help

Shows tool usage help.

Examples:  
- `FileParser -h`

### Extract

Extracts the translation keys and their value and generate a json file for each language.

| Option    |     Usage     | Description                                                                                     | Required |
| :-------- | :-----------: | :---------------------------------------------------------------------------------------------- | :------: |
| Input     |  -i, --input  | The path to the folder containing the BigFile_PC.d* files. Default value is the current folder. |    No    |
| Output    | -o, --output  | The path to the folder where the json files will be exported. Default value is `./output`.      |    No    |
| Verbosity | -v, --verbose | If set, the program will show details of warnings and error.                                    |    No    |

Examples:  
`FileParser`  
`FileParser -v -i ./input`  
`FileParser --input C:/Foo/Bar --output ./Baz/output`

Here's an example on Windows to use the files from the installed Steam version:  
`FileParser -i "C:\Program Files (x86)\Steam\steamapps\common\Detroit Become Human" -o "./output"`

> [!NOTE]
> The "Process buffers" step may seem to stay stuck at 0% for a few seconds. This is normal.