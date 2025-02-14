﻿// -----------------------------------------------------------------------
// <copyright file="FilterAffectedProjectFilesCmd.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Incrementalist.Git;
using Microsoft.Extensions.Logging;

namespace Incrementalist.ProjectSystem.Cmds
{
    /// <summary>
    ///     Filter the previously discovered <see cref="SlnFile" />s by whether or not
    ///     they were touched via the GitDiff.
    /// </summary>
    public sealed class
        FilterAffectedProjectFilesCmd : BuildCommandBase<Dictionary<string, SlnFile>, Dictionary<string, SlnFile>>
    {
        private readonly string _targetGitBranch;
        private readonly string _workingDirectory;

        public FilterAffectedProjectFilesCmd(ILogger logger, CancellationToken cancellationToken,
            string workingDirectory, string targetGitBranch)
            : base("FilterSlnFilesByGitDiff", logger, cancellationToken)
        {
            _workingDirectory = workingDirectory;
            _targetGitBranch = targetGitBranch;
        }

        protected override async Task<Dictionary<string, SlnFile>> ProcessImpl(
            Task<Dictionary<string, SlnFile>> previousTask)
        {
            var fileDictObj = await previousTask;

            var fileDict = (Dictionary<string, SlnFile>) fileDictObj;

            var repoResult = GitRunner.FindRepository(_workingDirectory);
            if (!repoResult.foundRepo)
            {
                Logger.LogError("Unable to find Git repository located in {0}. Shutting down.", _workingDirectory);
                return new Dictionary<string, SlnFile>();
            }

            // validate the target branch
            if (!DiffHelper.HasBranch(repoResult.repo, _targetGitBranch))
            {
                Logger.LogError("Current git repository doesn't have any branch named [{0}]. Shutting down.", _targetGitBranch);
                return new Dictionary<string, SlnFile>();
            }

            var repo = repoResult.repo;
            var affectedFiles = DiffHelper.ChangedFiles(repo, _targetGitBranch);

            // filter out any files that aren't affected by the diff
            var newDict = new Dictionary<string, SlnFile>();
            foreach (var file in affectedFiles)
            {
                Logger.LogDebug("Affected file: {0}", file);
                // this file is in the solution
                if (fileDict.ContainsKey(file)) newDict[file] = fileDict[file];
            }

            return newDict;
        }
    }
}