import { Controller, type Control, type FieldValues, type Path } from "react-hook-form";
import { StyleSheet, Text, TextInput, View } from "react-native";

import { colors } from "@/src/theme";

interface FormInputProps<T extends FieldValues> {
  name: Path<T>;
  control: Control<T>;
  label: string;
  placeholder?: string;
  secureTextEntry?: boolean;
  multiline?: boolean;
  autoCapitalize?: "none" | "sentences" | "words" | "characters";
}

export const FormInput = <T extends FieldValues>({
  name,
  control,
  label,
  placeholder,
  secureTextEntry,
  multiline,
  autoCapitalize = "none"
}: FormInputProps<T>) => {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field: { value, onChange, onBlur }, fieldState: { error } }) => (
        <View style={styles.container}>
          <Text style={styles.label}>{label}</Text>
          <TextInput
            value={String(value ?? "")}
            onChangeText={onChange}
            onBlur={onBlur}
            placeholder={placeholder}
            placeholderTextColor={colors.textMuted}
            selectionColor={colors.accentSoft}
            autoCapitalize={autoCapitalize}
            secureTextEntry={secureTextEntry}
            multiline={multiline}
            style={[styles.input, multiline ? styles.inputMultiline : null]}
          />
          {error ? <Text style={styles.error}>{error.message}</Text> : null}
        </View>
      )}
    />
  );
};

const styles = StyleSheet.create({
  container: {
    marginBottom: 16
  },
  label: {
    marginBottom: 4,
    fontSize: 14,
    fontWeight: "500",
    color: colors.textSecondary
  },
  input: {
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 16,
    paddingVertical: 12,
    color: colors.textPrimary
  },
  inputMultiline: {
    minHeight: 96,
    textAlignVertical: "top"
  },
  error: {
    marginTop: 4,
    fontSize: 12,
    color: colors.error
  }
});
