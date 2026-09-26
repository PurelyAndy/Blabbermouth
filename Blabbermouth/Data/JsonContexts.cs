using System.Collections.Generic;
using System.Text.Json.Serialization;
using Blabbermouth.Core;
using Blabbermouth.SttProviders;

namespace Blabbermouth.Data;

[JsonSerializable(typeof(SerialPayload))]
[JsonSerializable(typeof(SerialOperation))]
[JsonSerializable(typeof(ApiPayload))]
[JsonSerializable(typeof(List<Shocker>))]
[JsonSerializable(typeof(LexicalResult))]
[JsonSerializable(typeof(DetailedSpeechRecognitionResultCollection))]
[JsonSerializable(typeof(List<PhraseEntry>))]
public partial class JsonContext : JsonSerializerContext;