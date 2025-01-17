﻿// Copyright (c) Microsoft. All rights reserved.

using Shared.Models;

namespace SharedWebComponents.Models;

public readonly record struct AnswerResult<TRequest>(
    bool IsSuccessful,
    ChatAppResponseOrError? Response,
    Approach Approach,
    TRequest Request) where TRequest : ApproachRequest;
