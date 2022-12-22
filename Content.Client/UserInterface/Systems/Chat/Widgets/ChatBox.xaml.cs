﻿using Content.Client.Chat;
using Content.Client.Chat.TypingIndicator;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.Shared.Chat;
using Content.Shared.Input;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Audio;
using Robust.Shared.Input;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

[GenerateTypedNameReferences]
#pragma warning disable RA0003
public partial class ChatBox : UIWidget
#pragma warning restore RA0003
{
    private readonly ChatUIController _controller;

    public bool Main { get; set; }

    public ChatSelectChannel SelectedChannel => ChatInput.ChannelSelector.SelectedChannel;

    public ChatBox()
    {
        RobustXamlLoader.Load(this);

        ChatInput.Input.OnTextEntered += OnTextEntered;
        ChatInput.Input.OnKeyBindDown += OnKeyBindDown;
        ChatInput.Input.OnTextChanged += OnTextChanged;
        ChatInput.ChannelSelector.OnChannelSelect += OnChannelSelect;
        ChatInput.FilterButton.ChatFilterPopup.OnChannelFilter += OnChannelFilter;

        _controller = UserInterfaceManager.GetUIController<ChatUIController>();
        _controller.MessageAdded += OnMessageAdded;
        _controller.RegisterChat(this);
    }

    private void OnTextEntered(LineEditEventArgs args)
    {
        _controller.SendMessage(this, SelectedChannel);
    }

    private void OnMessageAdded(ChatMessage msg)
    {
        Logger.DebugS("chat", $"{msg.Channel}: {msg.Message}");
        if (!ChatInput.FilterButton.ChatFilterPopup.IsActive(msg.Channel))
        {
            return;
        }

        if (msg is { Read: false, AudioPath: { } })
            SoundSystem.Play(msg.AudioPath, Filter.Local(), new AudioParams().WithVolume(msg.AudioVolume));

        msg.Read = true;

        var color = msg.MessageColorOverride != null
            ? msg.MessageColorOverride.Value
            : msg.Channel.TextColor();

        AddLine(msg.WrappedMessage, color);
    }

    private void OnChannelSelect(ChatSelectChannel channel)
    {
        UpdateSelectedChannel();
    }

    private void OnChannelFilter(ChatChannel channel, bool active)
    {
        Contents.Clear();

        foreach (var message in _controller.History)
        {
            OnMessageAdded(message);
        }

        if (active)
        {
            _controller.ClearUnfilteredUnreads(channel);
        }
    }

    public void AddLine(string message, Color color)
    {
        var formatted = new FormattedMessage(3);
        formatted.PushColor(color);
        formatted.AddMarkup(message);
        formatted.Pop();
        Contents.AddMessage(formatted);
    }

    public void UpdateSelectedChannel()
    {
        var (prefixChannel, _) = _controller.SplitInputContents(ChatInput.Input.Text);
        var channel = prefixChannel == 0 ? SelectedChannel : prefixChannel;

        ChatInput.ChannelSelector.UpdateChannelSelectButton(channel);
    }

    public void Focus(ChatSelectChannel? channel = null)
    {
        var input = ChatInput.Input;
        var selectStart = Index.End;

        if (channel != null)
        {
            channel = _controller.MapLocalIfGhost(channel.Value);

            // Channel not selectable, just do NOTHING (not even focus).
            if ((_controller.SelectableChannels & channel.Value) == 0)
                return;

            var (_, text) = _controller.SplitInputContents(input.Text);

            var newPrefix = _controller.GetPrefixFromChannel(channel.Value);
            DebugTools.Assert(newPrefix != default, "Focus channel must have prefix!");

            if (channel == SelectedChannel)
            {
                // New selected channel is just the selected channel,
                // just remove prefix (if any) and leave text unchanged.

                input.Text = text.ToString();
                selectStart = Index.Start;
            }
            else
            {
                // Change prefix to new focused channel prefix and leave text unchanged.
                input.Text = string.Concat(newPrefix.ToString(), " ", text.Span);
                selectStart = Index.FromStart(2);
            }

            ChatInput.ChannelSelector.Select(channel.Value);
        }

        input.IgnoreNext = true;
        input.GrabKeyboardFocus();

        input.CursorPosition = input.Text.Length;
        input.SelectionStart = selectStart.GetOffset(input.Text.Length);
    }

    public void CycleChatChannel(bool forward)
    {
        var idx = Array.IndexOf(ChannelSelectorPopup.ChannelSelectorOrder, SelectedChannel);
        do
        {
            // go over every channel until we find one we can actually select.
            idx += forward ? 1 : -1;
            idx = MathHelper.Mod(idx, ChannelSelectorPopup.ChannelSelectorOrder.Length);
        } while ((_controller.SelectableChannels & ChannelSelectorPopup.ChannelSelectorOrder[idx]) == 0);

        SafelySelectChannel(ChannelSelectorPopup.ChannelSelectorOrder[idx]);
    }

    public void SafelySelectChannel(ChatSelectChannel toSelect)
    {
        toSelect = _controller.MapLocalIfGhost(toSelect);
        if ((_controller.SelectableChannels & toSelect) == 0)
            return;

        ChatInput.ChannelSelector.Select(toSelect);
    }

    private void OnKeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.TextReleaseFocus)
        {
            ChatInput.Input.ReleaseKeyboardFocus();
            ChatInput.Input.Clear();
            args.Handle();
            return;
        }

        if (args.Function == ContentKeyFunctions.CycleChatChannelForward)
        {
            CycleChatChannel(true);
            args.Handle();
            return;
        }

        if (args.Function == ContentKeyFunctions.CycleChatChannelBackward)
        {
            CycleChatChannel(false);
            args.Handle();
        }
    }

    private void OnTextChanged(LineEditEventArgs args)
    {
        // Update channel select button to correct channel if we have a prefix.
        UpdateSelectedChannel();

        // Warn typing indicator about change
        _controller.NotifyChatTextChange();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing) return;
        _controller.UnregisterChat(this);
        ChatInput.Input.OnTextEntered -= OnTextEntered;
        ChatInput.Input.OnKeyBindDown -= OnKeyBindDown;
        ChatInput.Input.OnTextChanged -= OnTextChanged;
        ChatInput.ChannelSelector.OnChannelSelect -= OnChannelSelect;
    }
}
