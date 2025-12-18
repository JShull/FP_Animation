# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0] - 2025-12-18

### 0.3.0 Changed

- [@JShull](https://github.com/jshull).
  - FPActivationMarker.cs
  - FPActivationMarkerDrawer.cs
  - FPActivationMarkerEditor.cs
  - FPActivationTimelineScrubDriver.cs
  - FPActivationReceiver.cs
    - Support for custom timeline scrubber activation/deactivation marker
    - Similar to Unity's Activation Track, but marker based

## [0.2.0] - 2025-12-16

### 0.2.0 Changed

- [@JShull](https://github.com/jshull).
  - FPSplineFollower.cs
    - Support for Yaw Only rotation (no pitch) along a path

### 0.2.0 Fixed

- [@JShull](https://github.com/jshull).
  - FPSplineCommandMarkerEditor.cs
    - Fixed missing reference to icon, now just points to null

## [0.1.0] - 2025-09-20

### 0.1.0 Added

- [@JShull](https://github.com/jshull).
  - Moved all test files to a Unity Package Distribution
  - ASMDEF for Editor
    - FPIKManagerEditor
    - FPSplineCommandMarkerEditor
    - FPTimelineAutoRefresh
  - ASMDEF for Runtime
    - FPAnimationInjector
    - FPSplineCommandMarker
    - FPSplineFollowBehaviour
    - FPSplineFollowClip
    - FPSplineFollower
    - FPSplineFollowerReceiver
    - FPSplineFollowTrack
    - FPIKManager

### 0.1.0 Changed

- None... yet

### 0.1.0 Fixed

- Setup the contents to align with Unity naming conventions

### 0.1.0 Removed

- None... yet
