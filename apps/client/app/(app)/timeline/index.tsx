import { Ionicons } from "@expo/vector-icons";
import { habitChecksCheck, habitChecksUncheck, habitsGetDaily, type DayItemResult } from "@twelve-daily/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as Haptics from "expo-haptics";
import { useEffect, useMemo, useRef, useState } from "react";
import { ActivityIndicator, Animated, Easing, Pressable, ScrollView, StyleSheet, Text, TouchableOpacity, View } from "react-native";

import { getApiErrorMessage } from "@/src/api/error";
import { buildHourRange, formatHourLabel, formatShortTime, formatTimelineDateLabel, formatTimelineDayLabel, parseTimeToMinutes, shiftIsoDate, toIsoDate } from "@/src/date";
import { colors } from "@/src/theme";
import { Screen } from "@/src/ui/screen";

const DEFAULT_START_HOUR = 0;
const DEFAULT_END_HOUR_EXCLUSIVE = 24;
const HOUR_HEIGHT = 128;
const MIN_CARD_HEIGHT = 56;
const CARD_LINE_HEIGHT = 18;
const POPOVER_WIDTH = 240;
const POPOVER_ESTIMATED_HEIGHT = 126;
const POPOVER_OFFSET = 10;

type DayPeriod = "Early morning" | "Morning" | "Afternoon" | "Night";

type ViewMode = "timeline" | "list";

interface PositionedTimelineItem {
  item: DayItemResult;
  key: string;
  top: number;
  height: number;
  column: number;
  totalColumns: number;
}

interface ParsedTimelineItem {
  item: DayItemResult;
  startMinutes: number;
  endMinutes: number;
}

const getParsedTimelineItems = (items: DayItemResult[]) => {
  const validItems: ParsedTimelineItem[] = [];
  const invalidItems: DayItemResult[] = [];

  items.forEach((item) => {
    const startMinutes = parseTimeToMinutes(item.startTime);
    const endMinutes = parseTimeToMinutes(item.endTime);

    if (startMinutes === null || endMinutes === null || endMinutes <= startMinutes) {
      invalidItems.push(item);
      return;
    }

    validItems.push({ item, startMinutes, endMinutes });
  });

  validItems.sort((left, right) => left.startMinutes - right.startMinutes);

  return { validItems, invalidItems };
};

const getTimelineItemKey = (item: DayItemResult) => item.habitId;

const getDayPeriod = (startMinutes: number, endMinutes: number): DayPeriod => {
  const midpoint = Math.floor((startMinutes + endMinutes) / 2);

  if (midpoint < 360) {
    return "Early morning";
  }

  if (midpoint < 720) {
    return "Morning";
  }

  if (midpoint < 1080) {
    return "Afternoon";
  }

  return "Night";
};

const clamp = (value: number, minimum: number, maximum: number) => Math.min(Math.max(value, minimum), maximum);

const getPositionedItems = (items: ParsedTimelineItem[], startHour: number): PositionedTimelineItem[] => {
  const activeItems: Array<{ renderedBottom: number; column: number; groupId: number }> = [];
  const groupColumns = new Map<number, number>();
  const positionedItems: Array<PositionedTimelineItem & { groupId: number }> = [];
  let currentGroupId = -1;

  items.forEach(({ item, startMinutes, endMinutes }) => {
    const top = ((startMinutes - (startHour * 60)) / 60) * HOUR_HEIGHT;
    const height = Math.max(((endMinutes - startMinutes) / 60) * HOUR_HEIGHT, MIN_CARD_HEIGHT);
    const renderedBottom = top + height;

    for (let index = activeItems.length - 1; index >= 0; index -= 1) {
      if (activeItems[index].renderedBottom <= top) {
        activeItems.splice(index, 1);
      }
    }

    if (activeItems.length === 0) {
      currentGroupId += 1;
    }

    const usedColumns = new Set(activeItems.map((entry) => entry.column));
    let column = 0;

    while (usedColumns.has(column)) {
      column += 1;
    }

    activeItems.push({ renderedBottom, column, groupId: currentGroupId });
    groupColumns.set(
      currentGroupId,
      Math.max(groupColumns.get(currentGroupId) ?? 0, ...activeItems.map((entry) => entry.column + 1))
    );

    positionedItems.push({
      item,
      key: getTimelineItemKey(item),
      top,
      height,
      column,
      totalColumns: 1,
      groupId: currentGroupId
    });
  });

  return positionedItems.map(({ groupId, ...item }) => ({
    ...item,
    totalColumns: groupColumns.get(groupId) ?? 1
  }));
};

export default function TimelineScreen() {
  const [date, setDate] = useState(() => toIsoDate(new Date()));
  const [viewMode, setViewMode] = useState<ViewMode>("timeline");
  const [actionError, setActionError] = useState<string | null>(null);
  const [selectedItemKey, setSelectedItemKey] = useState<string | null>(null);
  const [visiblePopoverKey, setVisiblePopoverKey] = useState<string | null>(null);
  const [now, setNow] = useState(() => new Date());
  const [agendaCanvasWidth, setAgendaCanvasWidth] = useState(0);
  const scrollViewRef = useRef<ScrollView>(null);
  const lastAutoScrolledDateRef = useRef<string | null>(null);
  const popoverAnimation = useRef(new Animated.Value(0)).current;
  const currentTimePulse = useRef(new Animated.Value(1)).current;
  const queryClient = useQueryClient();

  const timelineQuery = useQuery({
    queryKey: ["daily", date],
    queryFn: () => habitsGetDaily({ date })
  });

  const checkMutation = useMutation({
    mutationFn: async ({ habitId, isDone }: { habitId: string; isDone: boolean }) => {
      if (isDone) {
        await habitChecksUncheck(habitId, { date });
      } else {
        await habitChecksCheck(habitId, { date });
      }
    },
    onSuccess: async () => {
      setActionError(null);
      setSelectedItemKey(null);
      await Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["daily", date] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard"] })
      ]);
    },
    onError: (error) => {
      setActionError(getApiErrorMessage(error));
    }
  });

  const activeDay = timelineQuery.data?.days.find((day) => day.date === date);
  const timelineItems = useMemo(
    () => [...(activeDay?.items ?? [])],
    [activeDay?.items]
  );
  const { validItems: validTimelineItems } = useMemo(
    () => getParsedTimelineItems(timelineItems),
    [timelineItems]
  );
  // Reuse the same validated, start-time-sorted set as the timeline view so both
  // views render a consistent list of items (no malformed/zero-length entries).
  const listItems = useMemo(
    () => validTimelineItems.map((parsed) => parsed.item),
    [validTimelineItems]
  );
  const startHour = DEFAULT_START_HOUR;
  const endHourExclusive = DEFAULT_END_HOUR_EXCLUSIVE;
  const hourRange = useMemo(() => buildHourRange(startHour, endHourExclusive), [endHourExclusive, startHour]);
  const positionedItems = useMemo(() => getPositionedItems(validTimelineItems, startHour), [startHour, validTimelineItems]);
  const agendaHeight = useMemo(
    () => Math.max(
      (endHourExclusive - startHour) * HOUR_HEIGHT,
      positionedItems.reduce((largest, item) => Math.max(largest, item.top + item.height), 0)
    ),
    [endHourExclusive, positionedItems, startHour]
  );
  const visiblePopoverItem = useMemo(
    () => positionedItems.find((item) => item.key === visiblePopoverKey) ?? null,
    [positionedItems, visiblePopoverKey]
  );

  useEffect(() => {
    const timer = setInterval(() => {
      setNow(new Date());
    }, 60_000);

    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    const pulse = Animated.loop(
      Animated.sequence([
        Animated.timing(currentTimePulse, {
          toValue: 1.35,
          duration: 900,
          easing: Easing.inOut(Easing.quad),
          useNativeDriver: true
        }),
        Animated.timing(currentTimePulse, {
          toValue: 1,
          duration: 900,
          easing: Easing.inOut(Easing.quad),
          useNativeDriver: true
        })
      ])
    );

    pulse.start();

    return () => pulse.stop();
  }, [currentTimePulse]);

  useEffect(() => {
    if (selectedItemKey && !positionedItems.some((item) => item.key === selectedItemKey)) {
      setSelectedItemKey(null);
    }
  }, [positionedItems, selectedItemKey]);

  useEffect(() => {
    if (selectedItemKey) {
      setVisiblePopoverKey(selectedItemKey);
      popoverAnimation.setValue(0);
      Animated.parallel([
        Animated.timing(popoverAnimation, {
          toValue: 1,
          duration: 180,
          easing: Easing.out(Easing.cubic),
          useNativeDriver: true
        })
      ]).start();

      return;
    }

    if (!visiblePopoverKey) {
      return;
    }

    Animated.timing(popoverAnimation, {
      toValue: 0,
      duration: 120,
      easing: Easing.in(Easing.quad),
      useNativeDriver: true
    }).start(({ finished }) => {
      if (finished) {
        setVisiblePopoverKey(null);
      }
    });
  }, [popoverAnimation, selectedItemKey, visiblePopoverKey]);

  useEffect(() => {
    if (timelineQuery.isLoading || timelineQuery.isFetching || lastAutoScrolledDateRef.current === date) {
      return;
    }

    const today = new Date();
    const shouldFocusNow = date === toIsoDate(today);
    const targetY = shouldFocusNow
      ? Math.max((((today.getHours() * 60) + today.getMinutes()) / 60) * HOUR_HEIGHT - (HOUR_HEIGHT * 2), 0)
      : Math.max((positionedItems[0]?.top ?? 0) - HOUR_HEIGHT, 0);

    lastAutoScrolledDateRef.current = date;

    const frame = requestAnimationFrame(() => {
      scrollViewRef.current?.scrollTo({ y: targetY, animated: false });
    });

    return () => cancelAnimationFrame(frame);
  }, [date, positionedItems, timelineQuery.isFetching, timelineQuery.isLoading]);

  const isToday = date === toIsoDate(now);
  const currentMinutes = (now.getHours() * 60) + now.getMinutes();
  const showCurrentTimeIndicator = isToday && currentMinutes >= (startHour * 60) && currentMinutes <= (endHourExclusive * 60);
  const currentTimeTop = ((currentMinutes - (startHour * 60)) / 60) * HOUR_HEIGHT;
  const currentDateIso = toIsoDate(now);
  const navigationDayLabel = formatTimelineDayLabel(date, currentDateIso);
  const navigationDateLabel = formatTimelineDateLabel(date);

  const navigateToDate = (nextDate: string) => {
    if (nextDate === date) {
      return;
    }

    lastAutoScrolledDateRef.current = null;
    setActionError(null);
    setSelectedItemKey(null);
    setDate(nextDate);
  };

  const changeViewMode = (mode: ViewMode) => {
    // Clear any open timeline selection so the popover doesn't linger/reappear
    // when switching views.
    setSelectedItemKey(null);
    setViewMode(mode);
  };

  const selectedItemTimeRange = visiblePopoverItem
    ? `${formatShortTime(visiblePopoverItem.item.startTime)} - ${formatShortTime(visiblePopoverItem.item.endTime)}`
    : "";
  const canCheck = activeDay?.type !== "future";
  const selectedItemPeriod = visiblePopoverItem
    ? getDayPeriod(parseTimeToMinutes(visiblePopoverItem.item.startTime) ?? 0, parseTimeToMinutes(visiblePopoverItem.item.endTime) ?? 0)
    : null;
  const popoverWidth = Math.min(POPOVER_WIDTH, Math.max(agendaCanvasWidth - 24, 180));
  const popoverMetrics = useMemo(() => {
    if (!visiblePopoverItem || agendaCanvasWidth <= 0) {
      return null;
    }

    const leftInset = visiblePopoverItem.column > 0 ? 4 : 0;
    const rightInset = visiblePopoverItem.column < visiblePopoverItem.totalColumns - 1 ? 4 : 0;
    const cardLeft = ((visiblePopoverItem.column / visiblePopoverItem.totalColumns) * agendaCanvasWidth) + leftInset;
    const cardRight = agendaCanvasWidth - ((((visiblePopoverItem.column + 1) / visiblePopoverItem.totalColumns) * agendaCanvasWidth) - rightInset);
    const cardWidth = agendaCanvasWidth - cardLeft - cardRight;
    const cardCenter = cardLeft + (cardWidth / 2);
    const left = clamp(cardCenter - (popoverWidth / 2), 12, Math.max(12, agendaCanvasWidth - popoverWidth - 12));
    const placeAbove = visiblePopoverItem.top > (POPOVER_ESTIMATED_HEIGHT + POPOVER_OFFSET + 12);
    const top = placeAbove
      ? visiblePopoverItem.top - POPOVER_ESTIMATED_HEIGHT - POPOVER_OFFSET
      : Math.min(visiblePopoverItem.top + visiblePopoverItem.height + POPOVER_OFFSET, Math.max(12, agendaHeight - POPOVER_ESTIMATED_HEIGHT - 12));

    return {
      left,
      top,
      arrowLeft: clamp(cardCenter - left - 8, 18, popoverWidth - 18),
      placeAbove
    };
  }, [agendaCanvasWidth, agendaHeight, popoverWidth, visiblePopoverItem]);

  return (
    <Screen title="Timeline">
      <View style={styles.toolbar}>
        <TouchableOpacity
          style={styles.dayNavButton}
          activeOpacity={0.85}
          onPress={() => navigateToDate(shiftIsoDate(date, -1))}>
          <Ionicons name="chevron-back" size={20} color={colors.textPrimary} />
        </TouchableOpacity>

        <View style={styles.dayNavContent}>
          <Text style={styles.dayNavTitle}>{navigationDayLabel}</Text>
          <Text style={styles.dayNavSubtitle}>{navigationDateLabel}</Text>
        </View>

        <TouchableOpacity
          style={styles.dayNavButton}
          activeOpacity={0.85}
          onPress={() => navigateToDate(shiftIsoDate(date, 1))}>
          <Ionicons name="chevron-forward" size={20} color={colors.textPrimary} />
        </TouchableOpacity>
      </View>

      <View style={styles.viewToggle}>
        <TouchableOpacity
          style={[styles.viewToggleButton, viewMode === "timeline" ? styles.viewToggleButtonActive : null]}
          activeOpacity={0.85}
          onPress={() => changeViewMode("timeline")}>
          <Ionicons name="time-outline" size={16} color={viewMode === "timeline" ? colors.textPrimary : colors.textMuted} />
          <Text style={[styles.viewToggleText, viewMode === "timeline" ? styles.viewToggleTextActive : null]}>Timeline</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.viewToggleButton, viewMode === "list" ? styles.viewToggleButtonActive : null]}
          activeOpacity={0.85}
          onPress={() => changeViewMode("list")}>
          <Ionicons name="list-outline" size={16} color={viewMode === "list" ? colors.textPrimary : colors.textMuted} />
          <Text style={[styles.viewToggleText, viewMode === "list" ? styles.viewToggleTextActive : null]}>List</Text>
        </TouchableOpacity>
      </View>

      {timelineQuery.isLoading ? <ActivityIndicator color={colors.accentSoft} /> : null}
      {actionError ? <Text style={styles.errorText}>{actionError}</Text> : null}

      {viewMode === "list" ? (
        <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={styles.scrollContent}>
          {listItems.length === 0 && !timelineQuery.isLoading ? (
            <View style={styles.listEmptyState}>
              <Text style={styles.emptyText}>No habits for this date.</Text>
            </View>
          ) : null}

          {listItems.map((item) => {
            const isDone = !!item.checkedAt;

            return (
              <View
                key={item.habitId}
                style={[
                  styles.listRow,
                  isDone ? styles.listRowDone : null,
                  activeDay?.type === "future" ? styles.cardReadOnly : null
                ]}>
                <View style={styles.listTimeColumn}>
                  <Text style={[styles.listStartTime, isDone ? styles.listTextDone : null]}>{formatShortTime(item.startTime)}</Text>
                  <Text style={[styles.listEndTime, isDone ? styles.listTextDone : null]}>{formatShortTime(item.endTime)}</Text>
                </View>

                <View style={styles.listBody}>
                  <Text style={[styles.listTitle, isDone ? styles.cardTitleDone : null]}>
                    {item.emoji} {item.name}
                  </Text>
                  {item.description ? (
                    <Text style={[styles.listDescription, isDone ? styles.listTextDone : null]}>{item.description}</Text>
                  ) : null}
                </View>

                <TouchableOpacity
                  activeOpacity={0.85}
                  style={[
                    styles.listCheckButton,
                    isDone ? styles.listCheckButtonDone : null,
                    !canCheck || checkMutation.isPending ? styles.checkButtonDisabled : null
                  ]}
                  disabled={!canCheck || checkMutation.isPending}
                  onPress={() => checkMutation.mutate({ habitId: item.habitId, isDone })}>
                  <Ionicons
                    name={isDone ? "checkmark" : "ellipse-outline"}
                    size={20}
                    color={isDone ? colors.white : colors.textMuted}
                  />
                </TouchableOpacity>
              </View>
            );
          })}
        </ScrollView>
      ) : (
      <ScrollView ref={scrollViewRef} showsVerticalScrollIndicator={false} contentContainerStyle={styles.scrollContent}>
        <View style={[styles.agenda, { height: agendaHeight }] }>
          <View style={styles.timeRail}>
            {hourRange.map((hour) => (
              <View key={hour} style={[styles.hourRow, { height: HOUR_HEIGHT }]}>
                <Text style={styles.hourLabel}>{formatHourLabel(hour)}</Text>
              </View>
            ))}
          </View>

          <View style={styles.agendaCanvas} onLayout={({ nativeEvent }) => setAgendaCanvasWidth(nativeEvent.layout.width)}>
            {hourRange.map((hour) => (
              <View
                key={hour}
                style={[
                  styles.gridRow,
                  { top: (hour - startHour) * HOUR_HEIGHT, height: HOUR_HEIGHT }
                ]}>
                <View style={styles.gridLine} />
              </View>
            ))}

            {showCurrentTimeIndicator ? (
              <View style={[styles.currentTimeIndicator, { top: currentTimeTop }] }>
                <Animated.View
                  style={[
                    styles.currentTimeDot,
                    {
                      transform: [{ scale: currentTimePulse }],
                      opacity: currentTimePulse.interpolate({
                        inputRange: [1, 1.35],
                        outputRange: [0.8, 1]
                      })
                    }
                  ]}
                />
              </View>
            ) : null}

            {positionedItems.map(({ item, key, top, height, column, totalColumns }) => {
              const isDone = !!item.checkedAt;

              return (
                <TouchableOpacity
                  key={key}
                  activeOpacity={0.85}
                  style={[
                    styles.agendaCard,
                    {
                      top,
                      height,
                      left: `${(column / totalColumns) * 100}%`,
                      right: `${100 - (((column + 1) / totalColumns) * 100)}%`,
                      marginLeft: column > 0 ? 4 : 0,
                      marginRight: column < totalColumns - 1 ? 4 : 0
                    },
                    isDone ? styles.cardDone : null,
                    activeDay?.type === "future" ? styles.cardReadOnly : null,
                    selectedItemKey === key ? styles.cardSelected : null
                  ]}
                  onPress={() => setSelectedItemKey((current) => current === key ? null : key)}>
                  <Text style={[styles.cardTitle, isDone ? styles.cardTitleDone : null]} numberOfLines={2} ellipsizeMode="tail">
                    {item.emoji} {item.name}
                  </Text>
                </TouchableOpacity>
              );
            })}

            {visiblePopoverItem && popoverMetrics ? (
              <>
                <Pressable style={styles.dismissLayer} onPress={() => setSelectedItemKey(null)} />
                <Animated.View
                  style={[
                    styles.popover,
                    {
                      top: popoverMetrics.top,
                      left: popoverMetrics.left,
                      width: popoverWidth,
                      opacity: popoverAnimation,
                      transform: [
                        {
                          translateY: popoverAnimation.interpolate({
                            inputRange: [0, 1],
                            outputRange: [popoverMetrics.placeAbove ? 8 : -8, 0]
                          })
                        },
                        {
                          scale: popoverAnimation.interpolate({
                            inputRange: [0, 1],
                            outputRange: [0.96, 1]
                          })
                        }
                      ]
                    }
                  ]}>
                  <View
                    style={[
                      styles.popoverArrow,
                      popoverMetrics.placeAbove ? styles.popoverArrowDown : styles.popoverArrowUp,
                      { left: popoverMetrics.arrowLeft }
                    ]}
                  />
                  <View style={styles.popoverHeader}>
                    <Text style={styles.popoverTitle} numberOfLines={1}>
                      {visiblePopoverItem.item.emoji} {visiblePopoverItem.item.name}
                    </Text>
                  </View>
                  <Text style={styles.popoverInfo}>{selectedItemTimeRange}</Text>
                  <View style={styles.popoverFooter}>
                    <View>
                      <Text style={styles.popoverPeriodLabel}>Period</Text>
                      <Text style={styles.popoverPeriodValue}>{selectedItemPeriod}</Text>
                    </View>
                    <View style={styles.popoverActions}>
                      <TouchableOpacity
                        activeOpacity={0.85}
                        style={[
                          styles.checkButton,
                          !canCheck || checkMutation.isPending ? styles.checkButtonDisabled : null,
                          visiblePopoverItem.item.checkedAt ? styles.checkButtonDone : null
                        ]}
                        disabled={!canCheck || checkMutation.isPending}
                        onPress={() => {
                          checkMutation.mutate({
                            habitId: visiblePopoverItem.item.habitId,
                            isDone: !!visiblePopoverItem.item.checkedAt
                          });
                        }}>
                        <Text style={styles.checkButtonText}>{visiblePopoverItem.item.checkedAt ? "Undo" : "Check"}</Text>
                      </TouchableOpacity>
                    </View>
                  </View>
                </Animated.View>
              </>
            ) : null}

            {positionedItems.length === 0 ? (
              <View style={styles.emptyState}>
                <Text style={styles.emptyText}>No habits for this date.</Text>
              </View>
            ) : null}
          </View>
        </View>

      </ScrollView>
      )}
    </Screen>
  );
}

const styles = StyleSheet.create({
  toolbar: {
    marginBottom: 16,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12
  },
  dayNavButton: {
    width: 42,
    height: 42,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface
  },
  dayNavContent: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center"
  },
  dayNavTitle: {
    fontSize: 18,
    fontWeight: "700",
    color: colors.textPrimary
  },
  dayNavSubtitle: {
    marginTop: 2,
    color: colors.textSecondary
  },
  emptyText: {
    color: colors.textSecondary
  },
  errorText: {
    marginBottom: 12,
    color: colors.error
  },
  scrollContent: {
    paddingBottom: 24
  },
  agenda: {
    flexDirection: "row",
    alignItems: "flex-start"
  },
  timeRail: {
    width: 56,
    paddingRight: 8
  },
  hourRow: {
    alignItems: "flex-start"
  },
  hourLabel: {
    fontSize: 12,
    fontWeight: "600",
    color: colors.textMuted
  },
  agendaCanvas: {
    flex: 1,
    position: "relative",
    borderLeftWidth: 1,
    borderLeftColor: colors.border,
    overflow: "visible"
  },
  gridRow: {
    position: "absolute",
    left: 0,
    right: 0
  },
  gridLine: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    opacity: 0.45
  },
  agendaCard: {
    position: "absolute",
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 12,
    paddingVertical: 8,
    alignItems: "flex-start",
    justifyContent: "center",
    overflow: "hidden"
  },
  cardDone: {
    borderColor: colors.successBorder,
    backgroundColor: colors.successSoft
  },
  cardSelected: {
    borderColor: colors.accentSoft,
    shadowColor: colors.accentSoft,
    shadowOpacity: 0.2,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 4 },
    elevation: 3
  },
  cardReadOnly: {
    opacity: 0.8
  },
  emptyState: {
    position: "absolute",
    top: 24,
    left: 16,
    right: 16,
    alignItems: "center"
  },
  cardTitle: {
    fontSize: 14,
    lineHeight: CARD_LINE_HEIGHT,
    fontWeight: "600",
    color: colors.textPrimary,
    flexShrink: 1,
    width: "100%",
    textAlign: "left"
  },
  cardTitleDone: {
    color: colors.successText
  },
  currentTimeIndicator: {
    position: "absolute",
    left: 0,
    right: 0,
    height: 2,
    backgroundColor: colors.dangerStrong,
    zIndex: 2
  },
  currentTimeDot: {
    position: "absolute",
    left: -4,
    top: -3,
    width: 8,
    height: 8,
    borderRadius: 999,
    backgroundColor: colors.dangerStrong
  },
  dismissLayer: {
    ...StyleSheet.absoluteFillObject,
    zIndex: 4
  },
  popover: {
    position: "absolute",
    padding: 14,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceAlt,
    zIndex: 5,
    shadowColor: colors.background,
    shadowOpacity: 0.35,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 8 },
    elevation: 6
  },
  popoverArrow: {
    position: "absolute",
    width: 16,
    height: 16,
    backgroundColor: colors.surfaceAlt,
    borderLeftWidth: 1,
    borderTopWidth: 1,
    borderColor: colors.border,
    transform: [{ rotate: "45deg" }]
  },
  popoverArrowUp: {
    top: -8
  },
  popoverArrowDown: {
    bottom: -8,
    transform: [{ rotate: "225deg" }]
  },
  popoverTitle: {
    fontSize: 16,
    fontWeight: "700",
    color: colors.textPrimary,
    flex: 1,
    paddingRight: 12
  },
  popoverHeader: {
    flexDirection: "row",
    alignItems: "flex-start",
    justifyContent: "space-between",
    gap: 12
  },
  popoverInfo: {
    marginTop: 6,
    color: colors.textSecondary
  },
  popoverFooter: {
    marginTop: 12,
    flexDirection: "row",
    alignItems: "flex-end",
    justifyContent: "space-between",
    gap: 12
  },
  popoverActions: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8
  },
  popoverPeriodLabel: {
    fontSize: 11,
    fontWeight: "700",
    letterSpacing: 0.6,
    textTransform: "uppercase",
    color: colors.textMuted
  },
  popoverPeriodValue: {
    marginTop: 2,
    fontSize: 14,
    color: colors.textPrimary
  },
  checkButton: {
    minWidth: 84,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 10,
    backgroundColor: colors.accent,
    paddingHorizontal: 14,
    paddingVertical: 10
  },
  checkButtonDone: {
    backgroundColor: colors.successText
  },
  checkButtonDisabled: {
    opacity: 0.45
  },
  checkButtonText: {
    fontWeight: "700",
    color: colors.white
  },
  viewToggle: {
    flexDirection: "row",
    alignSelf: "flex-end",
    marginBottom: 16,
    padding: 4,
    gap: 4,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface
  },
  viewToggleButton: {
    flexDirection: "row",
    alignItems: "center",
    gap: 6,
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 8
  },
  viewToggleButtonActive: {
    backgroundColor: colors.surfaceAlt
  },
  viewToggleText: {
    fontSize: 13,
    fontWeight: "600",
    color: colors.textMuted
  },
  viewToggleTextActive: {
    color: colors.textPrimary
  },
  listEmptyState: {
    paddingTop: 24,
    alignItems: "center"
  },
  listRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    marginBottom: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface
  },
  listRowDone: {
    borderColor: colors.successBorder,
    backgroundColor: colors.successSoft
  },
  listTimeColumn: {
    width: 52,
    alignItems: "flex-start"
  },
  listStartTime: {
    fontSize: 14,
    fontWeight: "700",
    color: colors.textPrimary
  },
  listEndTime: {
    marginTop: 2,
    fontSize: 12,
    color: colors.textMuted
  },
  listTextDone: {
    color: colors.successText
  },
  listBody: {
    flex: 1
  },
  listTitle: {
    fontSize: 14,
    lineHeight: 19,
    fontWeight: "600",
    color: colors.textPrimary
  },
  listDescription: {
    marginTop: 2,
    fontSize: 12,
    lineHeight: 16,
    color: colors.textSecondary
  },
  listCheckButton: {
    width: 40,
    height: 40,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceAlt
  },
  listCheckButtonDone: {
    borderColor: colors.successText,
    backgroundColor: colors.successText
  }
});
