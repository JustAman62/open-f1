using UndercutF1.CodeGenerator;

await Terminal.OutLineAsync("Running JSON Schema Generator");

var generator = new JsonSchemaGenerator();
await generator.GenerateOptionsSchemaAsync();

await Terminal.OutLineAsync("Finished JSON Schema Generator");
