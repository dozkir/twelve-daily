import { Ionicons } from "@expo/vector-icons";
import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo } from "react";
import { useForm } from "react-hook-form";
import { ScrollView, StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { z } from "zod";

import { parseTimeToMinutes } from "@/src/date";
import {
  dayOptions,
  getDefaultHabitFormValues,
  type DayOption,
  type HabitFormValues
} from "@/src/habits/habit-form-values";
import { colors } from "@/src/theme";
import { FormInput } from "@/src/ui/form-input";
import { Screen } from "@/src/ui/screen";
import { TimeInput } from "@/src/ui/time-input";

const timeErrorMessage = "Use HH:mm";

const createTimeIssue = (
  value: string,
  path: ["startTime"] | ["endTime"] | ["daySchedules", DayOption, "startTime"] | ["daySchedules", DayOption, "endTime"],
  ctx: z.RefinementCtx
) => {
  if (parseTimeToMinutes(value) !== null) {
    return true;
  }

  ctx.addIssue({
    code: z.ZodIssueCode.custom,
    path,
    message: timeErrorMessage
  });

  return false;
};

const dayScheduleSchema = z.object({
  enabled: z.boolean(),
  startTime: z.string(),
  endTime: z.string()
});

export const habitFormSchema = z.object({
  name: z.string().min(1, "Name is required"),
  emoji: z.string().min(1, "Emoji is required"),
  description: z.string().optional(),
  useDifferentTimesByDay: z.boolean(),
  daysOfWeek: z.array(z.enum(dayOptions)),
  startTime: z.string(),
  endTime: z.string(),
  daySchedules: z.object({
    Sunday: dayScheduleSchema,
    Monday: dayScheduleSchema,
    Tuesday: dayScheduleSchema,
    Wednesday: dayScheduleSchema,
    Thursday: dayScheduleSchema,
    Friday: dayScheduleSchema,
    Saturday: dayScheduleSchema
  })
}).superRefine((values, ctx) => {
  if (values.useDifferentTimesByDay) {
    const enabledDays = dayOptions.filter((day) => values.daySchedules[day].enabled);

    if (enabledDays.length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["daysOfWeek"],
        message: "Select at least one day"
      });
    }

    enabledDays.forEach((day) => {
      const schedule = values.daySchedules[day];
      const startIsValid = createTimeIssue(schedule.startTime, ["daySchedules", day, "startTime"], ctx);
      const endIsValid = createTimeIssue(schedule.endTime, ["daySchedules", day, "endTime"], ctx);

      if (startIsValid && endIsValid && parseTimeToMinutes(schedule.endTime)! <= parseTimeToMinutes(schedule.startTime)!) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["daySchedules", day, "endTime"],
          message: "End time must be later than start time"
        });
      }
    });

    return;
  }

  if (values.daysOfWeek.length === 0) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["daysOfWeek"],
      message: "Select at least one day"
    });
  }

  const startIsValid = createTimeIssue(values.startTime, ["startTime"], ctx);
  const endIsValid = createTimeIssue(values.endTime, ["endTime"], ctx);

  if (startIsValid && endIsValid && parseTimeToMinutes(values.endTime)! <= parseTimeToMinutes(values.startTime)!) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["endTime"],
      message: "End time must be later than start time"
    });
  }
});

export { dayOptions, getDefaultHabitFormValues } from "@/src/habits/habit-form-values";
export type { HabitFormValues } from "@/src/habits/habit-form-values";

interface HabitFormProps {
  title: string;
  subtitle: string;
  initialValues?: HabitFormValues;
  submitLabel: string;
  submittingLabel: string;
  submitError?: string | null;
  isSubmitting?: boolean;
  footerNote?: string;
  onSubmit: (values: HabitFormValues) => Promise<void>;
  onCancel: () => void;
}

export const HabitForm = ({
  title,
  subtitle,
  initialValues,
  submitLabel,
  submittingLabel,
  submitError,
  isSubmitting = false,
  footerNote,
  onSubmit,
  onCancel
}: HabitFormProps) => {
  const resolvedInitialValues = useMemo(
    () => initialValues ?? getDefaultHabitFormValues(),
    [initialValues]
  );

  const { control, getValues, handleSubmit, setValue, watch, reset, formState } = useForm<HabitFormValues>({
    resolver: zodResolver(habitFormSchema),
    defaultValues: resolvedInitialValues
  });

  useEffect(() => {
    reset(resolvedInitialValues);
  }, [reset, resolvedInitialValues]);

  const selectedDays = watch("daysOfWeek");
  const useDifferentTimesByDay = watch("useDifferentTimesByDay");
  const daySchedules = watch("daySchedules");

  const toggleSharedDay = (day: DayOption) => {
    const isSelected = selectedDays.includes(day);
    const nextDays = isSelected
      ? selectedDays.filter((selectedDay) => selectedDay !== day)
      : [...selectedDays, day].sort((left, right) => dayOptions.indexOf(left) - dayOptions.indexOf(right));

    setValue("daysOfWeek", nextDays, { shouldDirty: true, shouldValidate: true });
  };

  const toggleDifferentTimesMode = () => {
    const nextValue = !useDifferentTimesByDay;
    const currentSelectedDays = getValues("daysOfWeek");
    const sharedStartTime = getValues("startTime");
    const sharedEndTime = getValues("endTime");

    if (nextValue) {
      dayOptions.forEach((day) => {
        const currentSchedule = getValues(`daySchedules.${day}`);
        const shouldEnable = currentSelectedDays.includes(day);

        setValue(`daySchedules.${day}.enabled`, shouldEnable, { shouldDirty: true });

        if (shouldEnable && !currentSchedule.enabled) {
          setValue(`daySchedules.${day}.startTime`, sharedStartTime, { shouldDirty: true });
          setValue(`daySchedules.${day}.endTime`, sharedEndTime, { shouldDirty: true });
        }
      });
    } else {
      const enabledDays = dayOptions.filter((day) => getValues(`daySchedules.${day}.enabled`));
      const normalizedDays = enabledDays.length > 0 ? enabledDays : currentSelectedDays;

      setValue("daysOfWeek", normalizedDays, { shouldDirty: true, shouldValidate: true });
    }

    setValue("useDifferentTimesByDay", nextValue, { shouldDirty: true, shouldValidate: true });
  };

  const togglePerDaySchedule = (day: DayOption) => {
    const currentSchedule = getValues(`daySchedules.${day}`);
    const nextEnabled = !currentSchedule.enabled;
    const sharedStartTime = getValues("startTime");
    const sharedEndTime = getValues("endTime");

    setValue(`daySchedules.${day}.enabled`, nextEnabled, { shouldDirty: true, shouldValidate: true });

    if (nextEnabled && !currentSchedule.enabled) {
      setValue(`daySchedules.${day}.startTime`, currentSchedule.startTime || sharedStartTime, { shouldDirty: true });
      setValue(`daySchedules.${day}.endTime`, currentSchedule.endTime || sharedEndTime, { shouldDirty: true });
    }

    const nextDays = nextEnabled
      ? [...selectedDays, day].sort((left, right) => dayOptions.indexOf(left) - dayOptions.indexOf(right))
      : selectedDays.filter((selectedDay) => selectedDay !== day);

    setValue("daysOfWeek", nextDays, { shouldDirty: true, shouldValidate: true });
  };

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values);
    } catch {
      // Parent handles the submission error state.
    }
  });

  return (
    <Screen title={title} subtitle={subtitle}>
      <ScrollView contentContainerStyle={styles.content}>
        <FormInput
          control={control}
          name="name"
          label="Name"
          placeholder="Morning workout"
          autoCapitalize="sentences"
        />
        <FormInput
          control={control}
          name="emoji"
          label="Emoji"
          placeholder="✨"
        />
        <FormInput
          control={control}
          name="description"
          label="Description"
          placeholder="Optional notes for this habit"
          autoCapitalize="sentences"
          multiline
        />

        <Text style={styles.sectionLabel}>Days of week</Text>
        <TouchableOpacity
          style={[styles.checkboxCard, useDifferentTimesByDay ? styles.checkboxCardActive : null]}
          onPress={toggleDifferentTimesMode}>
          <View style={[styles.checkboxBox, useDifferentTimesByDay ? styles.checkboxBoxChecked : null]}>
            {useDifferentTimesByDay ? <Ionicons name="checkmark" size={16} color={colors.white} /> : null}
          </View>
          <View style={styles.checkboxContent}>
            <Text style={styles.checkboxTitle}>Dias com horários diferentes</Text>
            <Text style={styles.checkboxSubtitle}>
              Configure um horário específico para cada dia habilitado.
            </Text>
          </View>
        </TouchableOpacity>

        {!useDifferentTimesByDay ? (
          <>
            <View style={styles.dayGrid}>
              {dayOptions.map((day) => {
                const isSelected = selectedDays.includes(day);

                return (
                  <TouchableOpacity
                    key={day}
                    style={[styles.dayChip, isSelected ? styles.dayChipSelected : null]}
                    onPress={() => toggleSharedDay(day)}>
                    <Text style={[styles.dayChipText, isSelected ? styles.dayChipTextSelected : null]}>{day.charAt(0)}</Text>
                  </TouchableOpacity>
                );
              })}
            </View>

            <View style={styles.timeRow}>
              <View style={styles.timeField}>
                <TimeInput control={control} name="startTime" label="Start" placeholder="07:00" />
              </View>
              <View style={styles.timeField}>
                <TimeInput control={control} name="endTime" label="End" placeholder="08:00" />
              </View>
            </View>
          </>
        ) : (
          <View style={styles.dayList}>
            {dayOptions.map((day) => {
              const schedule = daySchedules[day];

              return (
                <View key={day} style={[styles.dayListItem, schedule.enabled ? styles.dayListItemActive : null]}>
                  <TouchableOpacity style={styles.dayListHeader} onPress={() => togglePerDaySchedule(day)}>
                    <View style={[styles.checkboxBox, schedule.enabled ? styles.checkboxBoxChecked : null]}>
                      {schedule.enabled ? <Ionicons name="checkmark" size={16} color={colors.white} /> : null}
                    </View>
                    <Text style={[styles.dayListTitle, schedule.enabled ? styles.dayListTitleActive : null]}>{day}</Text>
                  </TouchableOpacity>

                  {schedule.enabled ? (
                    <View style={styles.dayTimeFields}>
                      <View style={styles.timeField}>
                        <TimeInput control={control} name={`daySchedules.${day}.startTime`} label="Start" placeholder="07:00" />
                      </View>
                      <View style={styles.timeField}>
                        <TimeInput control={control} name={`daySchedules.${day}.endTime`} label="End" placeholder="08:00" />
                      </View>
                    </View>
                  ) : null}
                </View>
              );
            })}
          </View>
        )}

        {formState.errors.daysOfWeek?.message ? (
          <Text style={styles.daysErrorText}>{formState.errors.daysOfWeek.message}</Text>
        ) : null}

        {submitError ? <Text style={styles.errorText}>{submitError}</Text> : null}

        <TouchableOpacity style={styles.primaryButton} onPress={submit}>
          <Text style={styles.primaryButtonText}>{isSubmitting ? submittingLabel : submitLabel}</Text>
        </TouchableOpacity>

        {footerNote ? <Text style={styles.footerNote}>{footerNote}</Text> : null}

        <TouchableOpacity style={styles.secondaryButton} onPress={onCancel}>
          <Text style={styles.secondaryButtonText}>Cancel</Text>
        </TouchableOpacity>
      </ScrollView>
    </Screen>
  );
};

const styles = StyleSheet.create({
  content: {
    paddingBottom: 32
  },
  sectionLabel: {
    marginBottom: 10,
    fontSize: 14,
    fontWeight: "500",
    color: colors.textSecondary
  },
  dayGrid: {
    flexDirection: "row",
    flexWrap: "nowrap",
    gap: 6,
    marginTop: 12
  },
  dayList: {
    marginTop: 12,
    gap: 10
  },
  dayListItem: {
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 14
  },
  dayListItemActive: {
    borderColor: colors.accentSoft,
    backgroundColor: colors.surfaceAlt
  },
  dayListHeader: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12
  },
  dayListTitle: {
    fontSize: 15,
    fontWeight: "600",
    color: colors.textSecondary
  },
  dayListTitleActive: {
    color: colors.textPrimary
  },
  dayTimeFields: {
    marginTop: 14,
    flexDirection: "row",
    gap: 12
  },
  daysErrorText: {
    marginTop: 8,
    marginBottom: 16,
    fontSize: 12,
    color: colors.error
  },
  dayChip: {
    flex: 1,
    minWidth: 0,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 0,
    paddingVertical: 10
  },
  dayChipSelected: {
    borderColor: colors.accentSoft,
    backgroundColor: colors.accentStrong
  },
  dayChipText: {
    textAlign: "center",
    fontWeight: "600",
    color: colors.textSecondary
  },
  dayChipTextSelected: {
    color: colors.white
  },
  checkboxCard: {
    flexDirection: "row",
    alignItems: "flex-start",
    gap: 12,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 16
  },
  checkboxCardActive: {
    borderColor: colors.accentSoft,
    backgroundColor: colors.surfaceAlt
  },
  checkboxBox: {
    width: 22,
    height: 22,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 6,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.background
  },
  checkboxBoxChecked: {
    borderColor: colors.accentSoft,
    backgroundColor: colors.accent
  },
  checkboxContent: {
    flex: 1
  },
  checkboxTitle: {
    fontWeight: "600",
    color: colors.textPrimary
  },
  checkboxSubtitle: {
    marginTop: 4,
    color: colors.textSecondary
  },
  timeRow: {
    flexDirection: "row",
    gap: 12,
    marginTop: 16
  },
  timeField: {
    flex: 1
  },
  footerNote: {
    marginTop: 12,
    fontSize: 13,
    lineHeight: 19,
    textAlign: "center",
    color: colors.textSecondary
  },
  errorText: {
    marginTop: 12,
    textAlign: "center",
    color: colors.error
  },
  primaryButton: {
    marginTop: 16,
    borderRadius: 12,
    backgroundColor: colors.accent,
    paddingHorizontal: 16,
    paddingVertical: 12
  },
  primaryButtonText: {
    textAlign: "center",
    fontWeight: "600",
    color: colors.white
  },
  secondaryButton: {
    marginTop: 12,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 16,
    paddingVertical: 12
  },
  secondaryButtonText: {
    textAlign: "center",
    fontWeight: "600",
    color: colors.textPrimary
  }
});

