import { Alert, Platform } from "react-native";

interface ConfirmOptions {
  title: string;
  message?: string;
  confirmText?: string;
  cancelText?: string;
  destructive?: boolean;
}

/**
 * Cross-platform confirmation dialog.
 *
 * On native uses `Alert.alert` with Cancel/Confirm buttons; on web falls back to
 * `window.confirm`, since `react-native-web`'s `Alert` is a no-op and never fires
 * the button callbacks. Resolves `true` when the user confirms, `false` otherwise.
 */
export function confirmAsync({
  title,
  message,
  confirmText = "OK",
  cancelText = "Cancel",
  destructive = false
}: ConfirmOptions): Promise<boolean> {
  if (Platform.OS === "web") {
    if (typeof window === "undefined" || typeof window.confirm !== "function") {
      return Promise.resolve(false);
    }
    const text = message ? `${title}\n\n${message}` : title;
    return Promise.resolve(window.confirm(text));
  }

  return new Promise((resolve) => {
    Alert.alert(
      title,
      message,
      [
        { text: cancelText, style: "cancel", onPress: () => resolve(false) },
        { text: confirmText, style: destructive ? "destructive" : "default", onPress: () => resolve(true) }
      ],
      { cancelable: true, onDismiss: () => resolve(false) }
    );
  });
}
