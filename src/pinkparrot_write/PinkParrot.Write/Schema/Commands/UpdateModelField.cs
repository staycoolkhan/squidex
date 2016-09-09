﻿// ==========================================================================
//  UpdateModelField.cs
//  PinkParrot Headless CMS
// ==========================================================================
//  Copyright (c) PinkParrot Group
//  All rights reserved.
// ==========================================================================

using PinkParrot.Core.Schema;

namespace PinkParrot.Write.Schema.Commands
{
    public class UpdateModelField : TenantCommand
    {
        public long FieldId { get; set; }

        public ModelFieldProperties Properties { get; set; }
    }
}