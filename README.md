# Shaman.StringUtils

Utilities for working with strings.

```csharp
using Shaman;

"hello world".ToPascalCase(); // HelloWorld
"hello world".ToTitleCase(); // Hello World

"Åå".RemoveAccentMarksAndToLower(); // aa
"HelloWorld".PascalCaseToNormalCase(); // Hello world
"HelloWorld".PascalCaseToCamelCase(); // helloWorld

StringUtils.GetWords("Hello, world!"); // new []{ "hello", "world" }

Func<string, bool> evaluator = StringUtils.ParseSearchQuery("hello world");

evaluator("Hi, Hello"); // false
evaluator("World, hello"); // true
evaluator("Helloworld"); // false

```