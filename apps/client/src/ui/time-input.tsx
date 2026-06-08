import DateTimePicker, { DateTimePickerAndroid, type DateTimePickerEvent } from "@react-native-community/datetimepicker";
import { useState } from "react";
import { Controller, type Control, type FieldValues, type Path } from "react-hook-form";
import { Platform, Pressable, StyleSheet, Text, TextInput, View } from "react-native";

import { formatShortTime, formatTimeValue, timeStringToDate } from "@/src/date";
import { colors } from "@/src/theme";

interface TimeInputProps<T extends FieldValues> {
  name: Path<T>;
  control: Control<T>;
  label: string;
  placeholder?: string;
}

export const TimeInput = <T extends FieldValues>({
  name,
  control,
  label,
  placeholder = "07:00"
}: TimeInputProps<T>) => {
  const [showIosPicker, setShowIosPicker] = useState(false);

  return (
    <Controller
      control={control}
      name={name}
      render={({ field: { value, onChange, onBlur }, fieldState: { error } }) => {
        const formattedValue = formatShortTime(typeof value === "string" ? value : "");
        const pickerValue = timeStringToDate(formattedValue || placeholder);

        const handlePickerChange = (event: DateTimePickerEvent, selectedDate?: Date) => {
          if (Platform.OS === "ios") {
            if (selectedDate) {
              onChange(formatTimeValue(selectedDate));
            }
            return;
          }

          if (event.type === "set" && selectedDate) {
            onChange(formatTimeValue(selectedDate));
          }
        };

        const openPicker = () => {
          if (Platform.OS === "android") {
            DateTimePickerAndroid.open({
              value: pickerValue,
              mode: "time",
              is24Hour: true,
              display: "clock",
              onChange: handlePickerChange
            });
            return;
          }

          if (Platform.OS === "ios") {
            setShowIosPicker(true);
          }
        };

        if (Platform.OS === "web") {
          return (
            <View style={styles.container}>
              <Text style={styles.label}>{label}</Text>
              <TextInput
                value={String(value ?? "")}
                onChangeText={onChange}
                onBlur={onBlur}
                placeholder={placeholder}
                placeholderTextColor={colors.textMuted}
                selectionColor={colors.accentSoft}
                keyboardType="numbers-and-punctuation"
                autoCapitalize="none"
                style={styles.input}
              />
              {error ? <Text style={styles.error}>{error.message}</Text> : null}
            </View>
          );
        }

        return (
          <View style={styles.container}>
            <Text style={styles.label}>{label}</Text>
            <Pressable
              style={({ pressed }) => [styles.pressableInput, pressed ? styles.pressableInputPressed : null]}
              onPress={openPicker}>
              <Text style={formattedValue ? styles.valueText : styles.placeholderText}>
                {formattedValue || placeholder}
              </Text>
            </Pressable>
            {showIosPicker ? (
              <View style={styles.iosPickerCard}>
                <DateTimePicker
                  value={pickerValue}
                  mode="time"
                  display="spinner"
                  is24Hour
                  onChange={handlePickerChange}
                />
                <Pressable style={styles.doneButton} onPress={() => setShowIosPicker(false)}>
                  <Text style={styles.doneButtonText}>Done</Text>
                </Pressable>
              </View>
            ) : null}
            {error ? <Text style={styles.error}>{error.message}</Text> : null}
          </View>
        );
      }}
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
  pressableInput: {
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 16,
    paddingVertical: 14
  },
  pressableInputPressed: {
    opacity: 0.85
  },
  valueText: {
    color: colors.textPrimary
  },
  placeholderText: {
    color: colors.textMuted
  },
  iosPickerCard: {
    marginTop: 8,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 8
  },
  doneButton: {
    alignSelf: "flex-end",
    borderRadius: 10,
    backgroundColor: colors.accent,
    paddingHorizontal: 14,
    paddingVertical: 8
  },
  doneButtonText: {
    fontWeight: "600",
    color: colors.white
  },
  error: {
    marginTop: 4,
    fontSize: 12,
    color: colors.error
  }
});

