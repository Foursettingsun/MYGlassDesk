using Mono.Cecil;
using Mono.Cecil.Cil;
sealed class TypeCopier(ModuleDefinition source, ModuleDefinition target)
{
    readonly Dictionary<string, TypeDefinition> types = new();
    readonly Dictionary<string, MethodDefinition> methods = new();
    readonly Dictionary<string, FieldDefinition> fields = new();
    IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> list) => list.SelectMany(t => new[] { t }.Concat(All(t.NestedTypes)));

    public void Copy() {
        var originals = All(source.Types.Where(t => t.Name != "<Module>")).ToArray();
        foreach (var type in originals) {
            var clone = new TypeDefinition(type.Namespace, type.Name, type.Attributes) { ClassSize=type.ClassSize, PackingSize=type.PackingSize };
            types.Add(type.FullName, clone);
            if (type.DeclaringType == null) target.Types.Add(clone); else types[type.DeclaringType.FullName].NestedTypes.Add(clone);
            if (type.HasGenericParameters) throw new InvalidOperationException("Repair helper declares unexpected generics.");
        }
        foreach (var type in originals) {
            var clone = types[type.FullName]; clone.BaseType = MapType(type.BaseType);
            foreach (var face in type.Interfaces) clone.Interfaces.Add(new InterfaceImplementation(MapType(face.InterfaceType)));
            foreach (var field in type.Fields) {
                var copy = new FieldDefinition(field.Name, field.Attributes, MapType(field.FieldType));
                if (field.HasConstant) copy.Constant=field.Constant;
                clone.Fields.Add(copy); fields.Add(field.FullName, copy);
            }
            foreach (var method in type.Methods) {
                if (method.HasGenericParameters) throw new InvalidOperationException("Repair helper declares unexpected generic methods.");
                var copy = new MethodDefinition(method.Name, method.Attributes, MapType(method.ReturnType)) { ImplAttributes=method.ImplAttributes, CallingConvention=method.CallingConvention };
                foreach (var param in method.Parameters) copy.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, MapType(param.ParameterType)));
                clone.Methods.Add(copy); methods.Add(method.FullName, copy);
            }
        }
        foreach (var type in originals) foreach (var method in type.Methods) if (method.HasBody) CopyBody(method, methods[method.FullName]);
    }

    TypeReference MapType(TypeReference type) {
        if (type == null) return null!;
        if (types.TryGetValue(type.FullName, out var own)) return own;
        var existingType = target.GetType(type.FullName);
        if (existingType != null) return existingType;
        if (type is GenericInstanceType generic) {
            var result = new GenericInstanceType(MapType(generic.ElementType));
            foreach (var argument in generic.GenericArguments) result.GenericArguments.Add(MapType(argument)); return result;
        }
        if (type is ArrayType array) return new ArrayType(MapType(array.ElementType), array.Rank);
        if (type is ByReferenceType reference) return new ByReferenceType(MapType(reference.ElementType));
        if (type is PointerType pointer) return new PointerType(MapType(pointer.ElementType));
        if (type is OptionalModifierType optional) return new OptionalModifierType(MapType(optional.ModifierType), MapType(optional.ElementType));
        if (type is RequiredModifierType required) return new RequiredModifierType(MapType(required.ModifierType), MapType(required.ElementType));
        if (type is GenericParameter) return type;
        return target.ImportReference(type);
    }

    MethodReference MapMethod(MethodReference method) {
        if (methods.TryGetValue(method.FullName, out var own)) return own;
        var existingType = target.GetType(method.DeclaringType.FullName);
        if (existingType != null) {
            var existingMethod = existingType.Methods.SingleOrDefault(m => m.FullName == method.FullName);
            if (existingMethod != null) return existingMethod;
        }
        if (method is GenericInstanceMethod generic) {
            var result = new GenericInstanceMethod(MapMethod(generic.ElementMethod));
            foreach (var argument in generic.GenericArguments) result.GenericArguments.Add(MapType(argument)); return result;
        }
        var imported = target.ImportReference(method);
        imported.DeclaringType=MapType(imported.DeclaringType);
        imported.ReturnType=MapType(imported.ReturnType);
        foreach (var param in imported.Parameters) param.ParameterType=MapType(param.ParameterType);
        return imported;
    }

    FieldReference MapField(FieldReference field) {
        if (fields.TryGetValue(field.FullName, out var own)) return own;
        var existingType = target.GetType(field.DeclaringType.FullName);
        if (existingType != null) {
            var existingField = existingType.Fields.SingleOrDefault(f => f.FullName == field.FullName);
            if (existingField != null) return existingField;
        }
        var imported=target.ImportReference(field);
        imported.DeclaringType=MapType(imported.DeclaringType); imported.FieldType=MapType(imported.FieldType); return imported;
    }

    void CopyBody(MethodDefinition original, MethodDefinition copy) {
        copy.Body = new MethodBody(copy) { InitLocals=original.Body.InitLocals, MaxStackSize=original.Body.MaxStackSize };
        var variables = new Dictionary<VariableDefinition, VariableDefinition>();
        foreach (var variable in original.Body.Variables) { var clone=new VariableDefinition(MapType(variable.VariableType)); copy.Body.Variables.Add(clone); variables.Add(variable,clone); }
        var instructions = new Dictionary<Instruction, Instruction>();
        foreach (var instruction in original.Body.Instructions) { var clone=Instruction.Create(OpCodes.Nop); clone.OpCode=instruction.OpCode; instructions.Add(instruction, clone); copy.Body.Instructions.Add(clone); }
        foreach (var instruction in original.Body.Instructions) {
            instructions[instruction].Operand = instruction.Operand switch {
                null => null, Instruction targetInstruction => instructions[targetInstruction], Instruction[] targets => targets.Select(i=>instructions[i]).ToArray(),
                MethodReference method => MapMethod(method), FieldReference field => MapField(field), TypeReference type => MapType(type),
                VariableDefinition variable => variables[variable], ParameterDefinition param => copy.Parameters[param.Index], _ => instruction.Operand
            };
        }
        foreach (var handler in original.Body.ExceptionHandlers) copy.Body.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType) {
            CatchType=handler.CatchType == null ? null : MapType(handler.CatchType),
            TryStart=instructions[handler.TryStart], TryEnd=handler.TryEnd == null ? null : instructions[handler.TryEnd],
            HandlerStart=instructions[handler.HandlerStart], HandlerEnd=handler.HandlerEnd == null ? null : instructions[handler.HandlerEnd],
            FilterStart=handler.FilterStart == null ? null : instructions[handler.FilterStart]
        });
    }
}
