---
name: NotifBlock
description: Traditional Chinese desktop notification dashboard prototype.
colors:
  Accent: "#2196FF"
  AccentSoft: "#2B7FFF33"
  AccentText: "#62BFFF"
  Green: "#34D399"
  GreenSoft: "#34D39926"
  GreenText: "#4ADE80"
  Danger: "#F87171"
  Bg: "#080F17"
  Panel: "#0C151E"
  PanelHead: "#0C161F"
  RowBg: "#111D27"
  RowHover: "#182938"
  RowActive: "#162E43"
  Field: "#111E29"
  Line: "#213240"
  LineSoft: "#1B2A36"
  LineBright: "#34516A"
  Text: "#E8EEF7"
  TextDim: "#ACBDD2"
  TextMute: "#99AFC5"
  TextFaint: "#8CA2BA"
  ToggleTrackOff: "#233241"
typography:
  body:
    fontFamily: "Microsoft JhengHei UI, PingFang TC, Heiti TC, Noto Sans CJK TC, Noto Sans TC, Segoe UI, Helvetica Neue, Inter, sans-serif"
    fontSize: "16px"
  headline:
    fontSize: "30px"
    fontWeight: 600
  section:
    fontSize: "22px"
    fontWeight: 600
  title:
    fontSize: "18px"
    fontWeight: 400
  label:
    fontSize: "14px"
  metric:
    fontSize: "30px"
    fontWeight: 700
rounded:
  panel: "14px"
  card: "12px"
  navigation: "11px"
  field: "10px"
  ghost: "9px"
components:
  panel:
    backgroundColor: "{colors.Panel}"
    rounded: "{rounded.panel}"
  card:
    backgroundColor: "{colors.RowBg}"
    rounded: "{rounded.card}"
  field:
    backgroundColor: "{colors.Field}"
    rounded: "{rounded.field}"
  button-ghost:
    textColor: "{colors.TextDim}"
    rounded: "{rounded.ghost}"
    padding: "6px 10px"
---

# Design System: NotifBlock

## Overview

The implemented dashboard uses a dark blue, three-column desktop composition with Traditional Chinese labels, restrained borders, and bright blue selection states. Preserve the existing vector `AppIcon` identity and the reference-derived information density. This documents a mock interface, not a working notification interception service.

Sources: `src/MessageRecord/Themes/Palette.axaml`, `Themes/Controls.axaml`, `Views/MainWindow.axaml`, and its code-behind. Frontmatter colors preserve the Avalonia resource names; alpha colors are converted from Avalonia ARGB to CSS RGBA order. Sizes describe Avalonia device-independent units, represented as pixels for token portability.

## Colors

Primary blue (`Accent`) marks active navigation, selected applications, tab underlines, enabled switches, and chart emphasis. `AccentSoft` supplies translucent selection fills; `AccentText` keeps accent labels legible on dark surfaces. Green communicates enabled or positive status; `Danger` supplies warning emphasis.

Neutral surfaces progress from `Bg` through `Panel` to `RowBg`, with `RowHover` and `RowActive` distinguishing interaction states. Use `Line`, `LineSoft`, and `LineBright` for structure. Preserve the four existing text levels rather than lowering opacity indiscriminately.

## Typography

Use the CJK-first body font stack throughout. Headings and metrics share a large scale but differ in weight. Section headings, application titles, body text, and compact metadata follow the frontmatter hierarchy. Navigation labels use the title size, becoming semibold when selected. Do not substitute icon-font glyphs for the existing vector icons.

## Layout

The default window is (1536 × 1024), with minimum dimensions (1280 × 760). An (88-unit) header holds the brand, application search, and window controls. Below it, proportional columns (`0.198*`, `0.319*`, `0.483*`) hold sidebar navigation, application selection, and detail content.

At window heights below (900), hide the miniature daily chart, reduce its card padding from (22) to (16 horizontal, 12 vertical), reduce the card bottom margin from (52) to (20), and reduce tool-navigation top spacing from (28) to (10). Preserve access to navigation at short desktop heights; this is not a mobile stacked layout. macOS uses system window controls and offsets the brand accordingly.

## Elevation & Depth

Depth comes from tonal layering and thin borders, without panel shadows. The daily chart uses a fading blue fill. Keep separation subtle so the selected item remains the strongest visual cue.

## Shapes

Use the established rounded panel, card, field, and navigation shapes. Most surfaces have a one-unit border. Icons use rounded strokes; switches use a pill track (56 × 34) and circular knob (22). Preserve the compact desktop proportions.

## Components

- Navigation pairs vector icons, labels, and optional counts. Selection adds a blue border and translucent fill; hover adds a lighter surface.
- Application rows use a blue outline and stronger surface when selected. Record rows use rounded cards with a subtle hover fill.
- Tabs communicate selection with accent text and a three-unit bottom border.
- Ghost buttons are quiet at rest; boxed variants add a field background and border. Keyboard focus adds a visible accent border. The close button turns red on hover.
- Search uses a rounded field containing a transparent text input, with an accent caret and selection fill.
- Switches animate the background and knob over (160 ms); navigation and row color changes use (120 ms). Exact motion and the compact-height breakpoint are recorded in `.impeccable/design.json`.

## Do's and Don'ts

- Do reuse the palette and shared controls when extending screens.
- Do retain readable Traditional Chinese text, vector branding, and visible keyboard focus.
- Do check the reference size and short windows at (800) and (760) height when altering sidebar content.
- Don't let the daily chart displace navigation in compact windows.
- Don't represent mock values or interface switches as proof of real system interception.
