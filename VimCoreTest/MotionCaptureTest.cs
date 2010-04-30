﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vim;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using VimCoreTest.Utils;
using Moq;
using Vim.Extensions;
using Microsoft.FSharp.Core;

namespace VimCoreTest
{
    [TestFixture]
    public class MotionCaptureTest
    {
        private SnapshotPoint _point;
        private Mock<IMotionUtil> _util;
        private MotionCapture _captureRaw;
        private IMotionCapture _capture;

        [SetUp]
        public void Create()
        {
            _point = Mock.MockObjectFactory.CreateSnapshotPoint(0);
            _util = new Mock<IMotionUtil>(MockBehavior.Strict);
            _captureRaw = new MotionCapture(_util.Object);
            _capture = _captureRaw;
        }

        internal MotionResult Process(string input, int? count)
        {
            var realCount = count.HasValue
                ? FSharpOption.Create(count.Value)
                : FSharpOption<int>.None;
            var res = _capture.ProcessInput(
                _point,
                InputUtil.CharToKeyInput(input[0]),
                realCount);
            foreach (var cur in input.Skip(1))
            {
                Assert.IsTrue(res.IsNeedMoreInput);
                var needMore = (MotionResult.NeedMoreInput)res;
                res = needMore.Item.Invoke(InputUtil.CharToKeyInput(cur));
            }

            return res;
        }

        internal void ProcessComplete(string input, int? count=null)
        {
            Assert.IsTrue(Process(input, count).IsComplete);
        }

        internal MotionData CreateMotionData()
        {
            return new MotionData(
                new SnapshotSpan(_point, _point),
                true,
                MotionKind.Inclusive,
                OperationKind.CharacterWise,
                FSharpOption.Create(42));
        }

        [Test]
        public void Word1()
        {
            _util
                .Setup(x => x.WordForward(WordKind.NormalWord, _point, 1))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("w", 1);
            _util.Verify();
        }

        [Test]
        public void Word2()
        {
            _util
                .Setup(x => x.WordForward(WordKind.NormalWord, _point, 2))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("w", 2);
        }

        [Test]
        public void BadInput()
        {
            var res = Process("z", 1);
            Assert.IsTrue(res.IsInvalidMotion);
            res = res.AsInvalidMotion().Item2.Invoke(InputUtil.VimKeyToKeyInput(VimKey.EscapeKey));
            Assert.IsTrue(res.IsCancel);
        }

        [Test, Description("Keep getting input until it's escaped")]
        public void BadInput2()
        {
            var res = Process("z", 1);
            Assert.IsTrue(res.IsInvalidMotion);
            res = res.AsInvalidMotion().Item2.Invoke(InputUtil.CharToKeyInput('a'));
            Assert.IsTrue(res.IsInvalidMotion);
            res = res.AsInvalidMotion().Item2.Invoke(InputUtil.VimKeyToKeyInput(VimKey.EscapeKey));
            Assert.IsTrue(res.IsCancel);
        }

        [Test]
        public void EndOfLine1()
        {
            _util
                .Setup(x => x.EndOfLine(_point,1))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("$", 1);
            _util.Verify();
        }

        [Test]
        public void EndOfLine2()
        {
            _util
                .Setup(x => x.EndOfLine(_point,2))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("$", 2);
            _util.Verify();
        }

        [Test]
        public void BeginingOfLine1()
        {
            _util
                .Setup(x => x.BeginingOfLine(_point))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("0", 1);
            _util.Verify();
        }

        [Test]
        public void FirstNonWhitespaceOnLine1()
        {
            _util
                .Setup(x => x.FirstNonWhitespaceOnLine(_point))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("^", 1);
            _util.Verify();
        }

        [Test]
        public void AllWord1()
        {
            _util
                .Setup(x => x.AllWord(WordKind.NormalWord, _point, 1))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("aw", 1);
            _util.Verify();
        }

        [Test]
        public void AllWord2()
        {
            _util
                .Setup(x => x.AllWord(WordKind.NormalWord, _point, 2))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("aw", 2);
            _util.Verify();
        }

        [Test]
        public void CharLeft1()
        {
            _util
                .Setup(x => x.CharLeft(_point, 1))
                .Returns(FSharpOption.Create(CreateMotionData()))
                .Verifiable();
            ProcessComplete("h",1);
            _util.Verify();
        }

        [Test]
        public void CharLeft2()
        {
            _util
                .Setup(x => x.CharLeft(_point, 2))
                .Returns(FSharpOption.Create(CreateMotionData()))
                .Verifiable();
            ProcessComplete("2h",1);
            _util.Verify();
        }

        [Test]
        public void CharRight1()
        {
            _util
                .Setup(x => x.CharRight(_point, 2))
                .Returns(FSharpOption.Create(CreateMotionData()))
                .Verifiable();
            ProcessComplete("2l",1);
            _util.Verify();
        }

        [Test]
        public void LineUp1()
        {
            _util
                .Setup(x => x.LineUp(_point, 1))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("k", 1);
            _util.Verify();
        }

        [Test]
        public void EndOfWord1()
        {
            _util
                .Setup(x => x.EndOfWord(WordKind.NormalWord, _point, 1))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("e", 1);
            _util.Verify();
        }

        public void EndOfWord2()
        {
            _util
                .Setup(x => x.EndOfWord(WordKind.BigWord, _point, 1))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("E", 1);
            _util.Verify();
        }

        [Test]
        public void ForwardChar1()
        {
            _util
                .Setup(x => x.ForwardChar('c', _point,1))
                .Returns(FSharpOption.Create(CreateMotionData()))
                .Verifiable();
            ProcessComplete("fc", 1);
            _util.Verify();
        }

        [Test]
        public void ForwardTillChar1()
        {
            _util
                .Setup(x => x.ForwardTillChar('c', _point,1))
                .Returns(FSharpOption.Create(CreateMotionData()))
                .Verifiable();
            ProcessComplete("tc", 1);
            _util.Verify();
        }

        [Test]
        public void BackwardCharMotion1()
        {
            _util
                .Setup(x => x.BackwardChar('c', _point,1))
                .Returns(FSharpOption.Create(CreateMotionData()))
                .Verifiable();
            ProcessComplete("Fc", 1);
            _util.Verify();
        }

        [Test]
        public void BackwardTillCharMotion1()
        {
            _util
                .Setup(x => x.BackwardTillChar('c', _point,1))
                .Returns(FSharpOption.Create(CreateMotionData()))
                .Verifiable();
            ProcessComplete("Tc", 1);
            _util.Verify();
        }

        [Test]
        public void Motion_G1()
        {
            _util
                .Setup(x => x.LineOrLastToFirstNonWhitespace(_point, FSharpOption<int>.None))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("G");
            _util.Verify();
        }

        [Test]
        public void Motion_G2()
        {
            _util
                .Setup(x => x.LineOrLastToFirstNonWhitespace(_point, FSharpOption.Create(1)))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("1G");
            _util.Verify();
        }

        [Test]
        public void Motion_G3()
        {
            _util
                .Setup(x => x.LineOrLastToFirstNonWhitespace(_point, FSharpOption.Create(42)))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("42G");
            _util.Verify();
        }

        [Test]
        public void Motion_gg1()
        {
            _util
                .Setup(x => x.LineOrFirstToFirstNonWhitespace(_point, FSharpOption<int>.None))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("gg");
            _util.Verify();
        }

        [Test]
        public void Motion_gg2()
        {
            _util
                .Setup(x => x.LineOrFirstToFirstNonWhitespace(_point, FSharpOption.Create(2)))
                .Returns(CreateMotionData())
                .Verifiable();
            ProcessComplete("2gg");
            _util.Verify();
        }
    }

}
