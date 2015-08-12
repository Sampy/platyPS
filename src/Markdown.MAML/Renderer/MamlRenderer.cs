﻿using System;
using System.Collections.Generic;
using System.Text;
using Markdown.MAML.Model.Markdown;
using Markdown.MAML.Model.MAML;
using Markdown.MAML.Parser;
using Markdown.MAML.Transformer;

namespace Markdown.MAML.Renderer
{
    /// <summary>
    /// This class contain logic to render maml from the Model.
    /// </summary>
    public class MamlRenderer
    {
        private StringBuilder _stringBuilder = new StringBuilder();
        private Stack<string> _tagStack = new Stack<string>();

        public const string XML_PREAMBULA = @"<?xml version=""1.0"" encoding=""utf-8""?>
<helpItems schema=""maml"">
";
        public const string COMMAND_PREAMBULA = @"<command:command xmlns:maml=""http://schemas.microsoft.com/maml/2004/10"" xmlns:command=""http://schemas.microsoft.com/maml/dev/command/2004/10"" xmlns:dev=""http://schemas.microsoft.com/maml/dev/2004/10"" xmlns:MSHelp=""http://msdn.microsoft.com/mshelp"">";

        private void PushTag(string tag)
        {
            _stringBuilder.AppendFormat("<{0}>{1}", tag, Environment.NewLine);
            _tagStack.Push(tag);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tagName">the name of tag</param>
        /// <param name="tag">content of tag, i.e. include parameters</param>
        private void PushTag(string tagName, string tag)
        {
            _stringBuilder.AppendFormat("<{0} {1}>", tagName, tag);
            _tagStack.Push(tagName);
        }
        
        private void PopTag(string tag)
        {
            string poped = _tagStack.Pop();
            if (poped != tag)
            {
                throw new FormatException("Expecting pop " + tag + ", but got " + poped);
            }
            _stringBuilder.AppendFormat("</{0}>{1}", tag, Environment.NewLine);
        }

        private void PopAllTags()
        {
            PopTag(_tagStack.Count);
        }

        private void PopTag(int count)
        {
            for (int i = 0; i < count; i++) 
            {
                _stringBuilder.AppendFormat("</{0}>{1}", _tagStack.Pop(), Environment.NewLine);
            }
        }

        /// <summary>
        /// This is a helper method to do all 3 steps.
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        public static string MarkdownStringToMamlString(string markdown)
        {
            var parser = new MarkdownParser();
            var transformer = new ModelTransformer();
            var renderer = new MamlRenderer();

            var markdownModel = parser.ParseString(markdown);
            var mamlModel = transformer.NodeModelToMamlModel(markdownModel);
            string maml = renderer.MamlModelToString(mamlModel);

            return maml;
        }

        public string MamlModelToString(IEnumerable<MamlCommand> mamlCommands)
        {
            _stringBuilder.Clear();
            _stringBuilder.AppendLine(XML_PREAMBULA);

            AddCommands(mamlCommands);

            _stringBuilder.AppendLine("</helpItems>");
            return _stringBuilder.ToString();
        }

        private void AddCommands(IEnumerable<MamlCommand> mamlCommands)
        {
            _stringBuilder.AppendLine(COMMAND_PREAMBULA);
            foreach (var command in mamlCommands)
            {
                PopAllTags();

                #region NAME, VERB, NOUN, + SYNOPSIS
                PushTag("command:details");
                _stringBuilder.AppendFormat("<command:name>{0}</command:name>{1}", command.Name, Environment.NewLine);
                var splittedName = command.Name.Split('-');
                var verb = splittedName[0];
                var noun = command.Name.Substring(verb.Length);
                _stringBuilder.AppendFormat("<command:verb>{0}</command:verb>{2}<command:noun>{1}</command:noun>{2}", verb, noun, Environment.NewLine);
                AddSynopsis(command);
                PopTag("command:details");
                #endregion // NAME, VERB, NOUN, + SYNOPSIS

                AddDescription(command);
                AddSyntax(command);
                AddParameters(command);
                AddInputs(command);
                AddOutputs(command);
                AddNotes(command);
                AddExamples(command);
                AddLinks(command);
            }
            _stringBuilder.AppendLine("</command:command>");
        }

        private void AddSyntax(MamlCommand command)
        {
            // TODO
        }

        private void AddLinks(MamlCommand command)
        {
            PushTag("command:RelatedLinks");
            foreach (MamlLink Link in command.Links)
            {
                PushTag("maml:NavigationLink");

                PushTag("maml:LinkText");
                _stringBuilder.AppendLine(Link.LinkName);
                PopTag(1);

                PushTag("maml:URI");
                _stringBuilder.AppendLine(Link.LinkUri);
                PopTag(1);

                PopTag(1);
            }
            PopTag(1);
        }

        private void AddExamples(MamlCommand command)
        {
            PushTag("command:examples");
            foreach (MamlExample example in command.Examples)
            {
                PushTag("command:example");

                PushTag("maml:title");
                _stringBuilder.AppendLine(example.Title);
                PopTag("maml:title");

                PushTag("dev:code");
                _stringBuilder.AppendLine(example.Code);
                PopTag("dev:code");

                PushTag("dev:remarks");
                AddParas(example.Remarks);
                PopTag("dev:remarks");

                PopTag("command:example");
            }
            PopTag("command:examples");
        }

        private void AddNotes(MamlCommand command)
        {
            PushTag("maml:alertSet");
            PushTag("maml:alert");
            AddParas(command.Notes);
            PopTag("maml:alert");
            PopTag("maml:alertSet");
        }

        private void AddOutputs(MamlCommand command)
        {
            PushTag("command:returnValues");
            foreach (var output in command.Outputs)
            {
                PushTag("command:returnValue");

                PushTag("dev:Type");
                PushTag("maml:name");
                _stringBuilder.AppendLine(output.TypeName);
                PopTag("maml:name");
                PopTag("dev:Type");

                PushTag("maml:Description");
                AddParas(output.Description);
                PopTag("maml:Description");

                PopTag("command:returnValue");
            }
            PopTag("command:returnValues");
        }

        private void AddInputs(MamlCommand command)
        {
            PushTag("command:inputTypes");
            foreach (var input in command.Inputs)
            {
                PushTag("command:inputType");

                PushTag("dev:Type");
                PushTag("maml:name");
                _stringBuilder.AppendLine(input.TypeName);
                PopTag("maml:name");
                PopTag("dev:Type");

                PushTag("maml:Description");
                AddParas(input.Description);
                PopTag("maml:Description");

                PopTag("command:inputType");
            }
            PopTag("command:inputTypes");
        }

        private void AddParameters(MamlCommand command)
        {
            PushTag("command:parameters");
            foreach (MamlParameter parameter in command.Parameters)
            {
                AddParameter(command, parameter);
            }
            PopTag("command:parameters");
        }

        private void AddParameter(MamlCommand command, MamlParameter parameter)
        {
            var attributes = "required=\"" + parameter.Required.ToString() + "\" " +
                             "variableLength=\"" + parameter.VariableLength.ToString() + "\" " +
                             "globbing=\"" + parameter.Globbing.ToString() + "\" " +
                             "pipelineInput=\"" + parameter.PipelineInput.ToString() + "\" " +
                             "position=\"" + parameter.Position + "\" " +
                             "Aliases=\"";
            int aliasCount = 0;
            foreach (string alias in parameter.Aliases)
            {
                attributes += alias + ", ";
                aliasCount++;
            }
            if (aliasCount > 0)
            {
                attributes = attributes.Substring(0, attributes.Length - 2);
            }
            attributes += "\"";

            PushTag("command:parameter", attributes);

            PushTag("maml:Name");
            _stringBuilder.AppendLine(parameter.Name);
            PopTag("maml:Name");

            PushTag("maml:Description");
            AddParas(parameter.Description);
            PopTag("maml:Description");

            attributes = "required=\"" + parameter.ValueRequired.ToString() + "\" " +
                         "variableLength=\"" + parameter.ValueVariableLength.ToString();
            attributes += "\"";

            PushTag("command:parameterValue", attributes);
            _stringBuilder.AppendLine(parameter.Type);
            PopTag("command:parameterValue");

            PopTag("command:parameter");
        }

        private void AddSynopsis(MamlCommand command)
        {
            PushTag("maml:description");
            AddParas(command.Synopsis);
            PopTag(1);
        }

        private void AddDescription(MamlCommand command)
        {
            PushTag("maml:description");
            AddParas(command.Description);
            PopTag(1);
        }

        private void AddParas(string Body)
        {
            if (Body != null)
            {
                string[] paragraphs = Body.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                foreach (string para in paragraphs)
                {
                    PushTag("maml:para");
                    _stringBuilder.AppendLine(para);
                    PopTag(1);
                }
            }
        }
    }
}