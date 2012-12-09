﻿using System.Linq;
using GammaJul.ReSharper.ForTea.Psi;
using GammaJul.ReSharper.ForTea.Tree;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.SelectEmbracingConstruct;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace GammaJul.ReSharper.ForTea.Services.Selection {

	/// <summary>
	/// Support for extend selection (Ctrl+W).
	/// </summary>
	[ProjectFileType(typeof(T4ProjectFileType))]
	public class T4SelectEmbracingConstructProvider : ISelectEmbracingConstructProvider {

		public bool IsAvailable(IPsiSourceFile sourceFile) {
			return sourceFile.Properties.ShouldBuildPsi;
		}

		public ISelectedRange GetSelectedRange(IPsiSourceFile sourceFile, DocumentRange documentRange) {
			Pair<IT4File, IFile> pair = GetFiles(sourceFile, documentRange);
			IT4File t4File = pair.First;
			IFile codeBehindFile = pair.Second;

			if (t4File == null)
				return null;

			ITreeNode t4Node = t4File.FindNodeAt(documentRange);
			if (t4Node == null)
				return null;
			
			// if the current selection is inside C# code, use the C# extend selection directly
			if (codeBehindFile != null) {
				ISelectEmbracingConstructProvider codeBehindProvider = PsiProjectFileTypeCoordinator.Instance
					.GetByPrimaryPsiLanguageType(codeBehindFile.Language)
					.SelectNotNull(fileType => Shell.Instance.GetComponent<IProjectFileTypeServices>().TryGetService<ISelectEmbracingConstructProvider>(fileType))
					.FirstOrDefault();

				if (codeBehindProvider != null) {
					ISelectedRange codeBehindRange = codeBehindProvider.GetSelectedRange(sourceFile, documentRange);
					if (codeBehindRange != null)
						return new T4CodeBehindWrappedSelection(t4File, codeBehindRange);
				}
			}
			
			return new T4NodeSelection(t4File, t4Node);
		}

		private static Pair<IT4File, IFile> GetFiles([NotNull] IPsiSourceFile sourceFile, DocumentRange documentRange) {
			IT4File primaryFile = null;
			IFile secondaryFile = null;
			
			foreach (IFile file in sourceFile.EnumeratePsiFiles(documentRange)) {
				var t4File = file as IT4File;
				if (t4File != null)
					primaryFile = t4File;
				else
					secondaryFile = file;
			}
			
			return Pair.Of(primaryFile, secondaryFile);
		}

	}

}