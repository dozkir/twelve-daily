import { useMemo, useState } from "react";
import { Controller, type Control, type FieldValues, type Path } from "react-hook-form";
import { FlatList, Modal, Pressable, StyleSheet, Text, TextInput, View } from "react-native";

import { colors } from "@/src/theme";
import { formatTimezoneOffset, getTimezones } from "@/src/timezones";

interface TimezoneSelectProps<T extends FieldValues> {
  name: Path<T>;
  control: Control<T>;
  label: string;
  placeholder?: string;
}

export const TimezoneSelect = <T extends FieldValues>({
  name,
  control,
  label,
  placeholder = "Select a timezone"
}: TimezoneSelectProps<T>) => {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");

  const zones = useMemo(() => getTimezones(), []);
  const filtered = useMemo(() => {
    const term = query.trim().toLowerCase();
    if (!term) {
      return zones;
    }
    return zones.filter((zone) => zone.toLowerCase().includes(term));
  }, [zones, query]);

  return (
    <Controller
      control={control}
      name={name}
      render={({ field: { value, onChange }, fieldState: { error } }) => {
        const current = value == null ? "" : String(value);

        const close = () => {
          setOpen(false);
          setQuery("");
        };

        const select = (zone: string) => {
          onChange(zone);
          close();
        };

        return (
          <View style={styles.container}>
            <Text style={styles.label}>{label}</Text>

            <Pressable
              style={({ pressed }) => [styles.field, pressed ? styles.fieldPressed : null]}
              onPress={() => setOpen(true)}>
              <Text style={current ? styles.valueText : styles.placeholderText} numberOfLines={1}>
                {current ? current.replace(/_/g, " ") : placeholder}
              </Text>
              {current ? <Text style={styles.offsetText}>{formatTimezoneOffset(current)}</Text> : null}
            </Pressable>

            {error ? <Text style={styles.error}>{error.message}</Text> : null}

            <Modal visible={open} animationType="slide" transparent onRequestClose={close}>
              <View style={styles.overlay}>
                <View style={styles.card}>
                  <View style={styles.header}>
                    <Text style={styles.title}>{label}</Text>
                    <Pressable onPress={close} hitSlop={8}>
                      <Text style={styles.done}>Done</Text>
                    </Pressable>
                  </View>

                  <TextInput
                    value={query}
                    onChangeText={setQuery}
                    placeholder="Search"
                    placeholderTextColor={colors.textMuted}
                    selectionColor={colors.accentSoft}
                    autoCapitalize="none"
                    autoCorrect={false}
                    style={styles.search}
                  />

                  <FlatList
                    data={filtered}
                    keyExtractor={(item) => item}
                    keyboardShouldPersistTaps="handled"
                    initialNumToRender={20}
                    style={styles.list}
                    renderItem={({ item }) => {
                      const selected = item === current;
                      return (
                        <Pressable
                          style={({ pressed }) => [styles.option, pressed ? styles.optionPressed : null]}
                          onPress={() => select(item)}>
                          <Text
                            style={[styles.optionText, selected ? styles.optionTextSelected : null]}
                            numberOfLines={1}>
                            {item.replace(/_/g, " ")}
                          </Text>
                          <Text style={styles.optionOffset}>{formatTimezoneOffset(item)}</Text>
                        </Pressable>
                      );
                    }}
                    ListEmptyComponent={<Text style={styles.empty}>No timezones found</Text>}
                  />
                </View>
              </View>
            </Modal>
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
  field: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 16,
    paddingVertical: 14
  },
  fieldPressed: {
    opacity: 0.85
  },
  valueText: {
    flexShrink: 1,
    color: colors.textPrimary
  },
  placeholderText: {
    flexShrink: 1,
    color: colors.textMuted
  },
  offsetText: {
    color: colors.textSecondary
  },
  error: {
    marginTop: 4,
    fontSize: 12,
    color: colors.error
  },
  overlay: {
    flex: 1,
    justifyContent: "flex-end",
    backgroundColor: "rgba(0, 0, 0, 0.5)"
  },
  card: {
    maxHeight: "75%",
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.background,
    paddingHorizontal: 16,
    paddingTop: 16,
    paddingBottom: 8
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    marginBottom: 12
  },
  title: {
    fontSize: 16,
    fontWeight: "600",
    color: colors.textPrimary
  },
  done: {
    fontSize: 15,
    fontWeight: "600",
    color: colors.accentSoft
  },
  search: {
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 16,
    paddingVertical: 12,
    color: colors.textPrimary,
    marginBottom: 8
  },
  list: {
    flexGrow: 0
  },
  option: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 14
  },
  optionPressed: {
    backgroundColor: colors.surfaceAlt
  },
  optionText: {
    flexShrink: 1,
    color: colors.textPrimary
  },
  optionTextSelected: {
    fontWeight: "700",
    color: colors.accentSoft
  },
  optionOffset: {
    fontSize: 13,
    color: colors.textMuted
  },
  empty: {
    paddingVertical: 24,
    textAlign: "center",
    color: colors.textMuted
  }
});
